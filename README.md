[![](https://img.shields.io/nuget/v/Soenneker.Tests.FixturedUnit.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Tests.FixturedUnit/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.tests.fixturedunit/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.tests.fixturedunit/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Tests.FixturedUnit.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Tests.FixturedUnit/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.tests.fixturedunit/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.tests.fixturedunit/actions/workflows/codeql.yml)

# Soenneker.Tests.FixturedUnit

A fundamental test that stores UnitFixture and provides synthetic inversion of control. It inherits from `UnitTest` and its most used function is `Resolve{T}`, which retrieves a service from the fixture service provider.

## Install

```bash
dotnet add package Soenneker.Tests.FixturedUnit
```

## Quick start

```csharp
using Soenneker.Tests.FixturedUnit.Abstract;

IFixturedUnitTest fixturedUnitTest = /* resolve from DI */;
var result = fixturedUnitTest.Resolve();
```

Resolves fixtured Unit Test.

## What you get

- `IFixturedUnitTest` — A fundamental test that stores UnitFixture and provides synthetic inversion of control. It inherits from `UnitTest` and its most used function is `Resolve{T}`, which retrieves a service from the fixture service provider.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `IFixturedUnitTest.Resolve(scoped)` | Resolves fixtured Unit Test. | The resulting value. |

## Practical notes

- Dispose instances you own when their scope ends so held resources can be released.
