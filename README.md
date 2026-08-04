# EventSourcing

EventSourcing is a small .NET event sourcing framework with runtime packages
and compile-time source generators.

The framework stores events, rebuilds aggregate state from event streams and
executes projections after successful writes. The source generators keep the
mapping, aggregation, repository and projection infrastructure explicit at
compile time instead of relying on reflection-heavy runtime discovery.

## Packages

| Package | Purpose |
| --- | --- |
| [`EventSourcing.Abstractions`](src/EventSourcing.Abstractions/README.md) | Contracts, marker attributes and shared abstractions for domain projects. |
| [`EventSourcing`](src/EventSourcing/README.md) | Runtime package with repositories, stores, EF Core integration and dependency injection support. |
| [`EventSourcing.SourceGenerators`](src/EventSourcing.SourceGenerators/README.md) | Source generators, analyzers and code fixes for repositories, mappers, aggregators, projectors and DI extensions. |
| [`EventSourcing.Publishers`](src/EventSourcing.Publishers/README.md) | Shared publisher abstractions for projection-based event publishing. |
| [`EventSourcing.Publishers.RabbitMQ`](src/EventSourcing.Publishers.RabbitMQ/README.md) | RabbitMQ publisher implementation for events and aggregate state. |

## Documentation

- [Getting Started](doc/GettingStarted.md)
- [Event Sourcing Concepts](doc/EventSourcingConcepts.md)
- [Benchmarks](doc/benchmarks/README.md)

## Quick Start

Install the runtime and source-generator package in the infrastructure project
that declares repositories.

```xml
<PackageReference Include="EventSourcing" Version="2.0.0" />
<PackageReference Include="EventSourcing.SourceGenerators" Version="2.0.0" PrivateAssets="all" />
```

Domain projects usually only need `EventSourcing.Abstractions`.

```xml
<PackageReference Include="EventSourcing.Abstractions" Version="2.0.0" />
```

Define events and aggregates in the domain layer, declare partial repositories
in the infrastructure layer and implement the generated projection classes.
The full flow is described in [Getting Started](doc/GettingStarted.md).

## Build

```powershell
dotnet restore
dotnet build
```

Pull request validation builds the solution and runs the non-RabbitMQ test
suite. Package publishing is handled separately from pull request validation.
