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
    /// <summary>
    /// Resolves fixtured Unit Test.
    /// </summary>
    /// <typeparam name="T">Type of value handled by the Fixtured Unit Test.</typeparam>
    /// <param name="scoped">Whether scoped.</param>
    /// <returns>The resulting value.</returns>
    T Resolve<T>(bool scoped = false);

    /// <summary>
    /// Creates scope for the Fixtured Unit Test.
    /// </summary>
    void CreateScope();

    /// <summary>
    /// Waits for on Queue To Empty for the Fixtured Unit Test.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the wait on queue to empty operation is complete.</returns>
    ValueTask WaitOnQueueToEmpty(CancellationToken cancellationToken = default);
}
