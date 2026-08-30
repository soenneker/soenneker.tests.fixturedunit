[![](https://img.shields.io/nuget/v/Soenneker.Tests.FixturedUnit.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Tests.FixturedUnit/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.tests.fixturedunit/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.tests.fixturedunit/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Tests.FixturedUnit.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Tests.FixturedUnit/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.tests.fixturedunit/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.tests.fixturedunit/actions/workflows/codeql.yml)

# Soenneker.Tests.FixturedUnit

An xUnit test base class that connects `UnitFixture` dependency injection, fake-data generators, injectable test logging, and background-queue draining.

## Installation

```bash
dotnet add package Soenneker.Tests.FixturedUnit
```

## Fixture setup

```csharp
using Microsoft.Extensions.DependencyInjection;
using Soenneker.Fixtures.Unit;
using Xunit;

public sealed class TestFixture : UnitFixture
{
    public TestFixture()
    {
        Services.AddSingleton<IClock, TestClock>();
        Services.AddScoped<OrderService>();
    }
}

[CollectionDefinition("unit")]
public sealed class UnitCollection : ICollectionFixture<TestFixture>;
```

## Test class

```csharp
using Soenneker.Tests.FixturedUnit;
using Xunit;
using Xunit.Abstractions;

[Collection("unit")]
public sealed class OrderServiceTests : FixturedUnitTest
{
    public OrderServiceTests(TestFixture fixture, ITestOutputHelper output)
        : base(fixture, output)
    {
    }

    [Fact]
    public async Task Creates_an_order()
    {
        OrderService service = Resolve<OrderService>(scoped: true);
        CreateOrder request = AutoFaker.Generate<CreateOrder>();

        Order result = await service.Create(request);

        Assert.NotEqual(Guid.Empty, result.Id);
    }
}
```

The constructor binds the fixture's injectable Serilog sink to the current `ITestOutputHelper`. `Faker` and `AutoFaker` reuse the instances owned by the fixture.

## Resolution and lifecycle

`Resolve<T>()` resolves from the fixture's root provider. Use `Resolve<T>(scoped: true)` for scoped services; the first scoped resolution creates one async scope that is reused for the test and disposed with the test base. `CreateScope()` is idempotent while that scope exists.

The fixture must have completed `InitializeAsync` before services can be resolved. Registrations belong in the fixture constructor, before its provider is built.

`WaitOnQueueToEmpty(cancellationToken)` resolves the fixture's shared `IBackgroundQueue` and waits until its queued work finishes. Use a bounded cancellation token so a failed producer cannot leave a test waiting indefinitely.

Because the output sink is shared by the fixture, avoid running test classes that inject different `ITestOutputHelper` instances through the same fixture concurrently; the most recent injection controls where fixture logs are written.
