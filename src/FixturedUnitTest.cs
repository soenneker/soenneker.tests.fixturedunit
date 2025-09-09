using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.XUnit.Injectable.Abstract;
using Soenneker.Extensions.ServiceProvider;
using Soenneker.Extensions.ValueTask;
using Soenneker.Fixtures.Unit;
using Soenneker.Tests.FixturedUnit.Abstract;
using Soenneker.Tests.Logging;
using Soenneker.Tests.Unit;
using Soenneker.Utils.BackgroundQueue.Abstract;
using Xunit;

namespace Soenneker.Tests.FixturedUnit;

///<inheritdoc cref="IFixturedUnitTest"/>
public class FixturedUnitTest : UnitTest, IFixturedUnitTest
{
    public UnitFixture Fixture { get; }

    public AsyncServiceScope? Scope { get; private set; }

    private readonly Lazy<IBackgroundQueue> _backgroundQueue;

    public FixturedUnitTest(UnitFixture fixture, ITestOutputHelper testOutputHelper) : base(testOutputHelper, fixture.AutoFaker)
    {
        Fixture = fixture;

        var outputSink = Resolve<IInjectableTestOutputSink>();
        outputSink.Inject(testOutputHelper);

        _backgroundQueue = new Lazy<IBackgroundQueue>(() => Resolve<IBackgroundQueue>(), LazyThreadSafetyMode.ExecutionAndPublication);

        LazyLogger = new Lazy<ILogger<LoggingTest>>(BuildLogger<FixturedUnitTest>, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Uses the static Serilog Log.Logger, and returns Microsoft ILogger after building a new one. Avoid if you can, utilize DI!
    /// Serilog should be configured with applicable sinks before calling this
    /// </summary>
    public static ILogger<T> BuildLogger<T>()
    {
        return new SerilogLoggerFactory(Log.Logger).CreateLogger<T>();
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

    public ValueTask WaitOnQueueToEmpty(CancellationToken cancellationToken = default)
    {
        return _backgroundQueue.Value.WaitUntilEmpty(cancellationToken);
    }

    public ValueTask InitializeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        if (Scope != null)
            await Scope.Value.DisposeAsync().NoSync();
    }
}