# EventSourcing

A small EventSourcing framework for .NET applications.

The runtime stores events with Entity Framework Core, rebuilds aggregate state
from event streams and executes projections after successful writes. It is
designed to be used together with `EventSourcing.SourceGenerators`, which
generates repositories, mappers, registries, aggregators, projectors and
dependency injection extensions at compile time.

## Installation

```xml
<PackageReference Include="EventSourcing" Version="2.0.0" />
<PackageReference Include="EventSourcing.SourceGenerators" Version="2.0.0" PrivateAssets="all" />
```

Use `EventSourcing.Abstractions` instead of `EventSourcing` in pure domain
projects when they only need contracts such as `IAggregate`, `IEvent` and
`[UseStateRepository]`.

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

Create a partial repository in an infrastructure project. The source generator
will implement the required runtime behavior.

```csharp
using EventSourcing.Repositories;
using MyShop.Domain.Orders;

namespace MyShop.Infrastructure.Orders;

[UseStateRepository(true)]
public partial class OrderRepository : IRepository<Order>
{
}
```

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

Register the generated EventSourcing services.

```csharp
using Microsoft.EntityFrameworkCore;

builder.Services.AddEventSourcing(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("EventStore"));
});
```

Initialize the database during application startup.

```csharp
var app = builder.Build();

app.Services.UseEventSourcing();
```

Use `IRepository<TAggregate>` from application code.

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

Update an aggregate by returning one or more events.

```csharp
public async Task<Result> RenameAsync(Guid orderId, string orderNumber, CancellationToken cancellationToken)
{
    var result = await _repository.UpdateAsync(
        orderId,
        order => new OrderRenamedEvent(order.Id, orderNumber, DateTimeOffset.UtcNow),
        cancellationToken);

    return result.ToResult();
}
```

## Generated components

`EventSourcing.SourceGenerators` reads partial `IRepository<TAggregate>`
implementations and generates:

- repository implementations
- event mappers
- serialization registries
- aggregators
- projections and projectors
- dependency injection extensions

The generated `AddEventSourcing` method wires these pieces together with the
runtime `EventStore`, `StateStore` and `EventStoreDbContext`.

## Projections

Projections are used to create read models from events. They allow you to
project the state of an aggregate into a format that is suitable for querying
and displaying in an application.

The source generator creates the base partial projection classes for each event
type. You must extend these partial classes and override `ProjectAsync` to
provide the projection logic. If you do not override `ProjectAsync`, the
default implementation returns a failed `Result`.

## State repository support

Add `[UseStateRepository(true)]` to a repository when the current aggregate
state should be stored beside the event stream.

```csharp
[UseStateRepository(true)]
public partial class OrderRepository : IRepository<Order>
{
}
```

The generator creates a state repository and state projector for the aggregate.
