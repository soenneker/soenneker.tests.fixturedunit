using System;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Tests.Unit;

namespace Soenneker.Tests.FixturedUnit.Abstract;

/// <summary>
/// A fundamental test that stores UnitFixture and provides synthetic inversion of control. <para/>
/// It inherits from <see cref="UnitTest"/> and its most used function is <see cref="Resolve{T}"/>,
/// which retrieves a service from the fixture service provider.
/// </summary>
public interface IFixturedUnitTest : IAsyncDisposable
{
    T Resolve<T>(bool scoped = false);

    void CreateScope();

    ValueTask WaitOnQueueToEmpty(CancellationToken cancellationToken = default);
}
