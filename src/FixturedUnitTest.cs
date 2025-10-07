using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
public abstract class FixturedUnitTest : UnitTest, IFixturedUnitTest
{
    public UnitFixture Fixture { get; }

    public AsyncServiceScope? Scope { get; private set; }

    private readonly Lazy<IBackgroundQueue> _backgroundQueue;

    public FixturedUnitTest(UnitFixture fixture, ITestOutputHelper testOutputHelper) : base(null, fixture.AutoFaker)
    {
        Fixture = fixture;

        var outputSink = Resolve<IInjectableTestOutputSink>();
        outputSink.Inject(testOutputHelper);

        LazyLogger = new Lazy<ILogger<LoggingTest>>(() => Resolve<ILogger<FixturedUnitTest>>(scoped: true), LazyThreadSafetyMode.ExecutionAndPublication);

        _backgroundQueue = new Lazy<IBackgroundQueue>(() => Resolve<IBackgroundQueue>(), LazyThreadSafetyMode.ExecutionAndPublication);
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

    public override async ValueTask DisposeAsync()
    {
        if (Scope != null)
        {
            await Scope.Value.DisposeAsync().NoSync();

            Scope = null;
        }

        await base.DisposeAsync()
            .NoSync();
    }
}