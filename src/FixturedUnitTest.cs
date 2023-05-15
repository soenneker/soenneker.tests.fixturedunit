using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.XUnit.Injectable.Abstract;
using Soenneker.Extensions.ServiceProvider;
using Soenneker.Fixtures.Unit;
using Soenneker.Tests.FixturedUnit.Abstract;
using Soenneker.Tests.Logging;
using Soenneker.Tests.Unit;
using Soenneker.Utils.BackgroundQueue.Abstract;
using Xunit.Abstractions;

namespace Soenneker.Tests.FixturedUnit;

///<inheritdoc cref="IFixturedUnitTest"/>
public class FixturedUnitTest : UnitTest, IFixturedUnitTest
{
    public UnitFixture Fixture { get; }

    public AsyncServiceScope? Scope { get; private set; }

    private readonly Lazy<IBackgroundQueue> _backgroundQueue;
    private readonly Lazy<IQueuedHostedService> _queuedHostedService;

    public FixturedUnitTest(UnitFixture fixture, ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;

        var outputSink = Resolve<IInjectableTestOutputSink>();
        outputSink.Inject(testOutputHelper);
        
        _backgroundQueue = new Lazy<IBackgroundQueue>(() => Resolve<IBackgroundQueue>(), LazyThreadSafetyMode.ExecutionAndPublication);
        _queuedHostedService = new Lazy<IQueuedHostedService>(() => Resolve<IQueuedHostedService>(), LazyThreadSafetyMode.ExecutionAndPublication);

        LazyLogger = new Lazy<ILogger<LoggingTest>>(BuildLogger<FixturedUnitTest>, LazyThreadSafetyMode.ExecutionAndPublication);
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

    public void CreateScope()
    {
        if (Fixture.ServiceProvider == null)
            throw new Exception("ServiceProvider was null trying create a scope!");

        Scope = Fixture.ServiceProvider.CreateAsyncScope();
    }
    
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

            (int tempTaskProcessingLength, int tempValueTaskProcessingLength) = await _queuedHostedService.Value.GetCountOfProcessingTasks();
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