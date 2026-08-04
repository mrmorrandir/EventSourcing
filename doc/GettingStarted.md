# Getting Started

This guide shows the common setup for using the EventSourcing runtime together
with `EventSourcing.SourceGenerators`.

The examples follow a CQRS-friendly, anemic aggregate style: aggregates hold
state and apply events, while command handlers contain validation and business
orchestration.

## Packages

Domain projects usually depend only on the abstractions package.

```xml
<PackageReference Include="EventSourcing.Abstractions" Version="2.0.0" />
```

Infrastructure projects that declare repositories need the runtime and source
generator packages.

```xml
<PackageReference Include="EventSourcing" Version="2.0.0" />
<PackageReference Include="EventSourcing.SourceGenerators" Version="2.0.0" PrivateAssets="all" />
```

Add `EventSourcing.Publishers.RabbitMQ` only when committed events or aggregate
state should be published to RabbitMQ.

```xml
<PackageReference Include="EventSourcing.Publishers.RabbitMQ" Version="2.0.0" />
```

## Domain Layer

Create events as records that implement `IEvent`. Each event describes a state
change that already happened.

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

public sealed record OrderDeletedEvent(
    Guid AggregateId,
    DateTimeOffset Timestamp) : IEvent;
```

Create an aggregate as a record that implements `IAggregate`. The static
`Create` method creates the initial state from the first event. `Apply` methods
return the next state for following events.

```csharp
using EventSourcing;

namespace MyShop.Domain.Orders;

public sealed record Order(
    Guid Id,
    string OrderNumber,
    bool IsDeleted,
    DateTimeOffset LastChanged) : IAggregate
{
    public static Order Create(OrderCreatedEvent @event)
    {
        return new Order(
            @event.AggregateId,
            @event.OrderNumber,
            false,
            @event.Timestamp);
    }

    public Order Apply(OrderRenamedEvent @event)
    {
        return this with
        {
            OrderNumber = @event.OrderNumber,
            LastChanged = @event.Timestamp
        };
    }

    public Order Apply(OrderDeletedEvent @event)
    {
        return this with
        {
            IsDeleted = true,
            LastChanged = @event.Timestamp
        };
    }
}
```

At this point, aggregate behavior can already be tested without any database or
repository infrastructure.

## Application Layer

Use command handlers to validate input, enforce business rules and create the
events that should be appended to the aggregate stream.

The repository API returns `FluentResults` results. Check `IsFailed` before
accessing `Value`.

```csharp
using EventSourcing.Repositories;
using FluentResults;
using MyShop.Domain.Orders;

namespace MyShop.Application.Orders;

public sealed class CreateOrderHandler
{
    private readonly IRepository<Order> _repository;

    public CreateOrderHandler(IRepository<Order> repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> HandleAsync(
        string orderNumber,
        CancellationToken cancellationToken)
    {
        var result = await _repository.CreateAsync(
            () => new OrderCreatedEvent(
                Guid.NewGuid(),
                orderNumber,
                DateTimeOffset.UtcNow),
            cancellationToken);

        if (result.IsFailed)
        {
            return result.ToResult<Guid>();
        }

        return Result.Ok(result.Value.Id);
    }
}
```

Update existing aggregates by returning one or more events from `UpdateAsync`.

```csharp
using EventSourcing.Repositories;
using FluentResults;
using MyShop.Domain.Orders;

namespace MyShop.Application.Orders;

public sealed class RenameOrderHandler
{
    private readonly IRepository<Order> _repository;

    public RenameOrderHandler(IRepository<Order> repository)
    {
        _repository = repository;
    }

    public async Task<Result> HandleAsync(
        Guid orderId,
        string orderNumber,
        CancellationToken cancellationToken)
    {
        var result = await _repository.UpdateAsync(
            orderId,
            order => new OrderRenamedEvent(
                order.Id,
                orderNumber,
                DateTimeOffset.UtcNow),
            cancellationToken);

        return result.ToResult();
    }
}
```

The examples keep business rules short on purpose. In real applications,
validate commands and query read models before creating events when a rule
depends on existing state outside the current aggregate.

## Infrastructure Layer

Create a partial repository in the infrastructure project. The source generator
will generate the implementation.

```csharp
using EventSourcing.Repositories;
using MyShop.Domain.Orders;

namespace MyShop.Infrastructure.Orders;

[UseStateRepository(true)]
public partial class OrderRepository : IRepository<Order>
{
}
```

`[UseStateRepository(true)]` tells the generator to create state repository and
state projector support in addition to the event stream repository.

For every valid repository, the source generator can create:

- repository implementations
- event mappers
- serialization registries
- aggregators
- projection classes and projectors
- optional state repositories
- dependency injection extensions

## Projections

Projections create read models from committed events. The source generator
creates one base partial projection class per aggregate/event combination.

You must implement the generated partial projection classes and override
`ProjectAsync`. If a generated projection is not overridden, the default
implementation returns a failed `Result`.

For an aggregate named `Order` and an event named `OrderCreatedEvent`, the
generated projection class is named `OrderOrderCreatedEventProjection`.

```csharp
using EventSourcing;
using FluentResults;
using Microsoft.Extensions.Logging;
using MyShop.Domain.Orders;

namespace MyShop.Infrastructure.Orders;

public partial class OrderOrderCreatedEventProjection
{
    private readonly ILogger<OrderOrderCreatedEventProjection> _logger;

    public OrderOrderCreatedEventProjection(
        ILogger<OrderOrderCreatedEventProjection> logger)
    {
        _logger = logger;
    }

    public override Task<Result> ProjectAsync(
        Order state,
        OrderCreatedEvent @event,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Projecting OrderCreatedEvent - State: {State}",
            state);

        return Task.FromResult(Result.Ok());
    }
}
```

Implement every generated projection that should participate in the write
pipeline. The projection is the right place to update read models, publish
state changes or trigger transport-specific projectors.

## Dependency Injection

Register EventSourcing in the application startup. The generated
`AddEventSourcing` extension wires repositories, mappers, aggregators,
projectors, stores and EF Core infrastructure.

```csharp
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEventSourcing(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("EventStore"));
});
```

For local development and tests, the runtime can use an in-memory EF Core
database.

```csharp
using Microsoft.EntityFrameworkCore.Diagnostics;

builder.Services.AddEventSourcing(options =>
{
    options
        .UseInMemoryDatabase("EventStore")
        .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
});
```

Initialize EventSourcing after the application has been built.

```csharp
var app = builder.Build();

app.Services.UseEventSourcing();

app.Run();
```

## API Layer

Expose commands through ASP.NET Core controllers, minimal APIs or another
application boundary. The API should translate HTTP input into commands and map
`Result` or `Result<T>` responses into HTTP responses.

```csharp
app.MapPost(
    "/orders",
    async (
        CreateOrderRequest request,
        CreateOrderHandler handler,
        CancellationToken cancellationToken) =>
    {
        var result = await handler.HandleAsync(
            request.OrderNumber,
            cancellationToken);

        if (result.IsFailed)
        {
            return Results.BadRequest(result.Errors.Select(error => error.Message));
        }

        return Results.Ok(result.Value);
    });
```

Keep HTTP-specific response mapping at the boundary. Do not serialize
`FluentResults` objects directly as public API contracts.

## RabbitMQ Publishing

Install `EventSourcing.Publishers.RabbitMQ` when events or aggregate state
should be published through RabbitMQ.

```csharp
builder.Services.AddRabbitMqPublishing(options =>
{
    options.UseConnection("localhost", "guest", "guest");
    options.UseBaseExchangeName("myshop");
});

builder.Services.AddRabbitMqEventPublisher<Order>();
builder.Services.AddRabbitMqStatePublisher<Order>();
```

Initialize RabbitMQ publishing before EventSourcing startup.

```csharp
var app = builder.Build();

await app.Services.UseRabbitMqPublishing();
app.Services.UseEventSourcing();

app.Run();
```

RabbitMQ publishers are registered as projectors. Generated read-model
projections are still separate and must still override `ProjectAsync`.
