using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.XUnit.Injectable.Abstract;
using Soenneker.Extensions.ServiceProvider;
using Soenneker.Fixtures.Unit;
using Soenneker.Tests.Logging;
using Soenneker.Tests.Unit;
using Soenneker.Utils.BackgroundQueue.Abstract;
using Xunit;
using Xunit.Abstractions;

namespace Soenneker.Tests.FixturedUnit;

/// <summary>
/// A fundamental xUnit test that stores UnitFixture and provides synthetic inversion of control. <para/>
/// It inherits from <see cref="UnitTest"/> and it's most used function is <see cref="Resolve{T}"/> which will reach out to the Fixture and retrieve a service from DI.
/// </summary>
public class FixturedUnitTest : UnitTest, IAsyncLifetime
{
    public UnitFixture Fixture { get; set; }

    public AsyncServiceScope? Scope { get; set; }

    private readonly Lazy<IBackgroundQueue> _backgroundQueue;
    private readonly Lazy<IQueuedHostedService> _queuedHostedService;

    public FixturedUnitTest(UnitFixture fixture, ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;

        var outputSink = Resolve<IInjectableTestOutputSink>();
        outputSink.Inject(testOutputHelper);
        
        _backgroundQueue = new Lazy<IBackgroundQueue>(() => Resolve<IBackgroundQueue>(), true);
        _queuedHostedService = new Lazy<IQueuedHostedService>(() => Resolve<IQueuedHostedService>(), true);

        LazyLogger = new Lazy<ILogger<LoggingTest>>(BuildLogger<FixturedUnitTest>, true);
    }

    /// <summary>
    /// Uses the static Serilog Log.Logger, and returns Microsoft ILogger after building a new one. Avoid if you can, utilize DI!
    /// Serilog should be configured with applicable sinks before calling this
    /// </summary>
    public static ILogger<T> BuildLogger<T>()
    {
        ILogger<T> logger = new SerilogLoggerFactory(Log.Logger).CreateLogger<T>();

        return logger;
    }

    /// <summary>
    /// Syntactic sugar for Factory.Services.Get();
    /// </summary>
    /// <remarks>Optionally, creates a scope if needed (if one doesn't already exist)</remarks>
    public T Resolve<T>(bool scoped = false)
    {
        if (Fixture.ServiceProvider == null)
            throw new Exception($"ServiceProvider was null trying to resolve service {typeof(T).Name}! Not able to resolve service");

        if (!scoped)
            return Fixture.ServiceProvider.Get<T>();

        if (Scope == null)
            CreateScope();

        return Scope!.Value.ServiceProvider.Get<T>();
    }

    /// <summary>
    /// Needed for resolving scoped services. Don't need to worry about disposal, the end of the test handles that.
    /// </summary>
    /// <remarks>Usually you'll want to use <see cref="Resolve{T}"/></remarks>
    public void CreateScope()
    {
        if (Fixture.ServiceProvider == null)
            throw new Exception("ServiceProvider was null trying create a scope!");

        Scope = Fixture.ServiceProvider.CreateAsyncScope();
    }
    
    /// <summary>
    /// Checks the background queue to see if it's empty, and loops until it is
    /// </summary>
    public async ValueTask WaitOnQueueToEmpty()
    {
        const int delayMs = 500;

        int valueTaskLength;
        int taskLength;
        int processingValueTaskLength;
        int processingTaskLength;

        do
        {
            (int tempTaskLength, int tempValueTaskLength) = _backgroundQueue.Value.GetLengthsOfQueues();
            taskLength = tempTaskLength;
            valueTaskLength = tempValueTaskLength;

            (int tempTaskProcessingLength, int tempValueTaskProcessingLength) = _queuedHostedService.Value.GetCountOfProcessingTasks();
            processingTaskLength = tempValueTaskProcessingLength;
            processingValueTaskLength = tempTaskProcessingLength;

            if (valueTaskLength > 0 || taskLength > 0 || processingValueTaskLength > 0 || processingTaskLength > 0)
            {
                Logger.LogDebug("Waiting {delayMs}ms for Background queue to empty...", delayMs);

                await Delay(delayMs, null, false);
            }
            else
            {
                Logger.LogDebug("Background queue is empty; continuing");
            }
        } while (valueTaskLength > 0 || taskLength > 0 || processingValueTaskLength > 0 || processingTaskLength > 0);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        GC.SuppressFinalize(this);

        if (Scope != null)
            await Scope.Value.DisposeAsync();
    }
}