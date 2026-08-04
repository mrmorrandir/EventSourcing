# EventSourcing.Abstractions

Contracts and marker attributes for the EventSourcing framework.

Install this package when a project should define aggregates, events,
repository contracts or source-generator markers without referencing the
runtime persistence implementation.

## Package role

`EventSourcing.Abstractions` contains:

- `IAggregate`
- `IEvent`
- repository and store contracts
- mapper contracts
- projection contracts
- `[UseStateRepository]`

The package is intentionally small. Domain projects should usually depend on
this package, while infrastructure projects depend on `EventSourcing` and
`EventSourcing.SourceGenerators`.

## Tutorial

Create an event by implementing `IEvent`.

```csharp
using EventSourcing;

namespace MyShop.Domain.Orders;

public sealed record OrderCreatedEvent(
    Guid AggregateId,
    string OrderNumber,
    DateTimeOffset Timestamp) : IEvent;
```

Create an aggregate by implementing `IAggregate`.

```csharp
using EventSourcing;

namespace MyShop.Domain.Orders;

public sealed record Order(
    Guid Id,
    string OrderNumber,
    DateTimeOffset LastChanged) : IAggregate
{
    public static Order Create(OrderCreatedEvent @event)
    {
        return new Order(@event.AggregateId, @event.OrderNumber, @event.Timestamp);
    }
}
```

In an infrastructure project, define a partial repository and use the full
runtime and source-generator packages.

```csharp
using EventSourcing.Repositories;
using MyShop.Domain.Orders;

namespace MyShop.Infrastructure.Orders;

[UseStateRepository(true)]
public partial class OrderRepository : IRepository<Order>
{
}
```

The `[UseStateRepository(true)]` marker tells the source generators to create
state repository and state projector support for the aggregate.
