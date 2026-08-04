# EventSourcing.Publishers

Shared publisher abstractions for the EventSourcing framework.

This package is used by transport-specific publisher packages. Application
projects usually install a concrete implementation such as
`EventSourcing.Publishers.RabbitMQ` instead of referencing this package
directly.

## Package role

`EventSourcing.Publishers` provides `IPublisher<TAggregate>`. A publisher is a
projector that receives aggregate state and committed events after repository
operations.

## Tutorial

Create a custom publisher by implementing `IPublisher<TAggregate>`.

```csharp
using EventSourcing;
using EventSourcing.Publishers;
using FluentResults;
using MyShop.Domain.Orders;

namespace MyShop.Infrastructure.Orders;

public sealed class OrderAuditPublisher : IPublisher<Order>
{
    public Task<Result> ProjectAsync(
        Order state,
        IEvent @event,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok());
    }
}
```

Register the publisher as an `IProjector<TAggregate>` so the generated
repository can call it after events are saved.

```csharp
using EventSourcing.Projections;
using MyShop.Domain.Orders;
using MyShop.Infrastructure.Orders;

builder.Services.AddScoped<IProjector<Order>, OrderAuditPublisher>();
```

For RabbitMQ publishing, prefer the ready-made
`EventSourcing.Publishers.RabbitMQ` package.
