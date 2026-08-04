# EventSourcing.SourceGenerators

Compile-time generators, analyzers and code fixes for the EventSourcing
framework.

Install this package in infrastructure projects that declare partial
repositories. The package generates repository implementations, event mappers,
serialization registries, aggregators, projectors and dependency injection
extensions.

## Installation

```xml
<PackageReference Include="EventSourcing" Version="2.0.0" />
<PackageReference Include="EventSourcing.SourceGenerators" Version="2.0.0" PrivateAssets="all" />
```

`EventSourcing.SourceGenerators` is a compile-time package. It is delivered as
an analyzer asset and does not add runtime assemblies to your application.

## Tutorial

Create events and an aggregate in your domain project.

```csharp
using EventSourcing;

namespace MyShop.Domain.Orders;

public sealed record OrderCreatedEvent(
    Guid AggregateId,
    string OrderNumber,
    DateTimeOffset Timestamp) : IEvent;

public sealed record OrderRenamedEvent(
    Guid AggregateId,
    string OrderNumber,
    DateTimeOffset Timestamp) : IEvent;

public sealed record Order(
    Guid Id,
    string OrderNumber,
    DateTimeOffset LastChanged) : IAggregate
{
    public static Order Create(OrderCreatedEvent @event)
    {
        return new Order(@event.AggregateId, @event.OrderNumber, @event.Timestamp);
    }

    public Order Apply(OrderRenamedEvent @event)
    {
        return this with
        {
            OrderNumber = @event.OrderNumber,
            LastChanged = @event.Timestamp
        };
    }
}
```

Create a partial repository in your infrastructure project.

```csharp
using EventSourcing.Repositories;
using MyShop.Domain.Orders;

namespace MyShop.Infrastructure.Orders;

[UseStateRepository(true)]
public partial class OrderRepository : IRepository<Order>
{
}
```

Build the project. The generators create the implementation and registration
code for the repository, mapper registry, aggregator and projectors.

Implement the generated projections. The generator creates one partial
projection class per event type, but the projection logic must be provided by
your application.

```csharp
using EventSourcing;
using FluentResults;
using Microsoft.Extensions.Logging;
using MyShop.Domain.Orders;

namespace MyShop.Infrastructure.Orders;

public partial class OrderOrderCreatedEventProjection
{
    private readonly ILogger<OrderOrderCreatedEventProjection> _logger;

    public OrderOrderCreatedEventProjection(ILogger<OrderOrderCreatedEventProjection> logger)
    {
        _logger = logger;
    }

    public override Task<Result> ProjectAsync(
        Order state,
        OrderCreatedEvent @event,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Projecting OrderCreatedEvent - State: {State}", state);
        return Task.FromResult(Result.Ok());
    }
}
```

Register the generated services.

```csharp
using Microsoft.EntityFrameworkCore;

builder.Services.AddEventSourcing(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("EventStore"));
});
```

Use the repository from application code.

```csharp
using EventSourcing.Repositories;
using FluentResults;
using MyShop.Domain.Orders;

public sealed class CreateOrderHandler
{
    private readonly IRepository<Order> _repository;

    public CreateOrderHandler(IRepository<Order> repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> HandleAsync(string orderNumber, CancellationToken cancellationToken)
    {
        var result = await _repository.CreateAsync(
            () => new OrderCreatedEvent(Guid.NewGuid(), orderNumber, DateTimeOffset.UtcNow),
            cancellationToken);

        if (result.IsFailed)
        {
            return result.ToResult<Guid>();
        }

        return Result.Ok(result.Value.Id);
    }
}
```

## Generated APIs

For every valid repository, the package can generate:

- a concrete repository implementation
- an optional state repository
- event mappers
- an `ISerializationRegistry<TAggregate>` implementation
- an `IAggregator<TAggregate>` implementation
- projection classes and projectors
- `AddEventSourcing`, `AddRepositories`, `AddSerialization`, `AddAggregators`
  and `AddProjectors`

The bundled analyzers warn when repository classes are not partial, do not
follow the expected repository naming convention or generated projections still
need a `ProjectAsync` override.

## Projections

Projections are used to create read models from events. They allow you to
project the state of an aggregate into a format that is suitable for querying
and displaying in an application.

The source generator creates the base partial projection classes for each event
type. You must extend these partial classes and override `ProjectAsync` to
provide the projection logic. If you do not override `ProjectAsync`, the
default implementation returns a failed `Result`.
