# EventSourcing

This library provides a framework for implementing event sourcing in applications. It allows you to capture state changes as events, which can be stored and replayed to reconstruct the state of an application at any point in time.

This library is best used together with the `EventSourcing.SourceGenerators` package, which provides source generators to simplify the creation of event-sourced entities.

To start using this library together with the source generators, add both `EventSourcing` and `EventSourcing.SourceGenerators` packages to your project.

This library will support an anemic approach for the aggregates and events, meaning that the aggregate will not contain any business logic, and the events will only contain data. The business logic should be implemented in the application layer, which will use the aggregates and events to perform operations.

<!-- TOC -->
* [EventSourcing](#eventsourcing)
  * [Domain Layer](#domain-layer)
  * [Application Layer](#application-layer)
  * [Infrastructure Layer](#infrastructure-layer)
    * [Repository Implementation](#repository-implementation)
    * [Projections](#projections)
  * [API Layer](#api-layer)
    * [Dependency Injection](#dependency-injection)
<!-- TOC -->

## Domain Layer

Starting with the `MyTestAggregate` example, you can create an aggregate that represents a business entity. The aggregate will have methods to apply events and to handle commands.

First the events: each event represents a state change in the aggregate. You can define events as records that implement the `IEvent` interface.

```csharp
using EventSourcing;

namespace MyTestProject.Domain;

public record CreatedEvent(Guid AggregateId, string Name, string Description, DateTimeOffset Timestamp) : IEvent;
public record ChangedNameEvent(Guid AggregateId, string Name, DateTimeOffset Timestamp) : IEvent;
public record ChangedDescriptionEvent(Guid AggregateId, string Description, DateTimeOffset Timestamp) : IEvent;
public record DeletedEvent(Guid AggregateId, DateTimeOffset Timestamp) : IEvent;
```

Next, you can define the aggregate that will use these events to maintain its state. The aggregate will implement the `IAggregate` interface and provide methods to apply the events.

```csharp
using EventSourcing;

namespace MyTestProject.Domain;

public record MyTestAggregate(Guid Id, string Name, string Description, bool IsDeleted, DateTimeOffset LastChanged) : IAggregate
{
    public static MyTestAggregate Create(CreatedEvent @event) => new(@event.AggregateId, @event.Name, @event.Description, false, @event.Timestamp);
    
    public MyTestAggregate Apply(ChangedNameEvent nameEvent) => this with
    {
        Name = nameEvent.Name,
        LastChanged = nameEvent.Timestamp
    };
    
    public MyTestAggregate Apply(ChangedDescriptionEvent descriptionEvent) => this with
    {
        Description = descriptionEvent.Description,
        LastChanged = descriptionEvent.Timestamp
    };
    
    public MyTestAggregate Apply(DeletedEvent deleteEvent) => this with
    {
        IsDeleted = true,
        LastChanged = deleteEvent.Timestamp
    };
}
```

At this point, you can already create an instance of the aggregate and apply events to it. This allows you to create unit tests for the aggregate to ensure that it behaves as expected.

## Application Layer

In the application layer, you can implement the business logic that uses the aggregates and events. This layer will handle commands and orchestrate the creation and modification of aggregates. 

The application layer will typically use a CQRS (Command Query Responsibility Segregation) approach, where commands are used to change the state of the application and queries are used to retrieve the state.

A command handler will be responsible for handling a command and applying the necessary events to the aggregate. Here is an example of a command handler that creates a new `MyTestAggregate`.  
In this example, we will be using the `Mediator.Abstractions` and `Mediator.SourceGenerator` packages ([GitHub martinothamar/Mediator](https://github.com/martinothamar/Mediator)) to implement the mediator pattern (you can also use other libraries like `MediatR`). 
The package `FluentResults` is used to return errors and results in a fluent way - instead of throwing exceptions ([GitHub altmann/FluentResults](https://github.com/altmann/FluentResults))

```csharp
using EventSourcing.Repositories;
using FluentResults;
using Mediator;
using MyTestProject.Domain

namespace EventSourcing.SourceGenerators.Target.Application;

public record CreateCommand(string Name, string Description) : IRequest<Result<Guid>>;

public class CreateCommandHandler : IRequestHandler<CreateCommand, Result<Guid>>
{
    private readonly IRepository<MyTestAggregate> _repository;

    public CreateCommandHandler(IRepository<MyTestAggregate> repository)
    {
        _repository = repository;
    }

    public async ValueTask<Result<Guid>> Handle(CreateCommand request, CancellationToken cancellationToken)
    {
        // ... do validation in the pipeline or here ...
        // ... check if the name is not already taken by using a read-projections-repository or a database context ...
        
        var createResult = await _repository.CreateAsync(() => new CreatedEvent(Guid.NewGuid(), request.Name, request.Description, DateTimeOffset.UtcNow), cancellationToken);
        if (createResult.IsFailed)
            return new Error("Failed to create aggregate").CausedBy(createResult.Errors);
        
        var aggregate = createResult.Value;
        return Result.Ok(aggregate.Id);
    }
}
```

The interesting part here is the `CreateAsync` method of the repository, which will create a new aggregate and apply the event to it. The repository will handle the storage of the aggregate and its events. We will talk about the projections and repositories in the next section.

We need to talk about the `UpdateAsync` method of the repository, which will be used to update an existing aggregate. This method will apply the events to the aggregate and save it back to the repository.

```csharp
using EventSourcing.Repositories;
using FluentResults;
using Mediator;
using MyTestProject.Domain

namespace EventSourcing.SourceGenerators.Target.Application;

public record ChangeNameCommand(Guid AggregateId, string Name) : IRequest<Result>;

public class ChangeNameCommandHandler : IRequestHandler<ChangeNameCommand, Result>
{
    private readonly IRepository<MyTestAggregate> _repository;

    public ChangeNameCommandHandler(IRepository<MyTestAggregate> repository)
    {
        _repository = repository;
    }

    public async ValueTask<Result> Handle(ChangeNameCommand request, CancellationToken cancellationToken)
    {
        // ... do validation in the pipeline or here ...
        // ... check if the name is not already taken by using a read-projections-repository or a database context ...

        var updateResult = await _repository.UpdateAsync(request.AggregateId, (aggregate) => [new ChangedNameEvent(aggregate.Id, request.Name, DateTimeOffset.UtcNow)], cancellationToken);
        if (updateResult.IsFailed)
            return new Error("Failed to update aggregate").CausedBy(updateResult.Errors);
        
        // var updatedAggregate = updateResult.Value;
        
        return Result.Ok();
    }
}
```

The `UpdateAsync` method takes the aggregate ID and a function that returns a list of events to apply to the aggregate. The repository will retrieve the aggregate, apply the events, and save it back to the storage.

**Remarks:**
- The `IRepository<T>` interface is a generic repository interface that will be implemented in the infrastructure layer (with the help of the source generators). It will handle the storage of aggregates and their events.
- The `CreateAsync` and `UpdateAsync` methods are designed to work with the events and aggregates, allowing you to create and update aggregates consistently.
- The `CreateAsync` and `UpdateAsync` methods both have overloads that will accept an `async` to return the events / list of events, which can be useful for more complex scenarios where you need to perform additional operations before returning the events.
- The examples above use the `FluentResults` library to return results and errors fluently. This is not mandatory, but it is a good practice to avoid throwing exceptions for expected errors.
- The **examples** above **do not provide business logic** in the command handlers for **simplicity**. In a real application, you would typically have validation and business rules implemented in the command handlers. First: **read** operations which would be used against the projected read models (projections in a database or in-memory) to ensure that the commands are valid and do not violate any business rules.
- The examples only `return` the aggregate's id or a result, but you can also return any other data you need, such as DTOs (Data Transfer Objects) or view models, depending on your application's architecture.

## Infrastructure Layer

In the infrastructure layer, you will implement the repositories and projections. The repositories will handle the storage of aggregates and their events, while the projections will provide read models for queries.

You can use the `EventSourcing.SourceGenerators` package to generate the repository implementations automatically. The source generator will create a repository for each aggregate type you define, allowing you to focus on the business logic without worrying about the storage details.

In addition, the source generator will also generate default event mappers — that will be used to serialize and deserialize the events together with a versioned schema name to be stored int he `IEventStore` - and projections — that will be used to create read models from the events.

### Repository Implementation

To create all the stuff needed for the infrastructure layer, you need to do several (complicated) steps:

```csharp
using EventSourcing.Repositories;
using EventSourcing.SourceGenerators.Target.Domain;

namespace MyTestProject.Infrastructure;

public partial class MyTestAggregateRepository : IRepository<MyTestAggregate>
{
    
}
```

**Yeah, that's it** (I mean apart from the projections). The source generator will generate the implementation of the `IRepository<MyTestAggregate>` interface for you, mostly by inheriting from the `Repository<MyTestAggregate>` class and creating the required constructor.

Furthermore, the source generator will create: 
- the default event mappers (implementing `AbstractEventMapper<T>` for each event)
  - `MyTestAggregateCreatedEventMapper`
  - `MyTestAggregateChangedNameEventMapper`
  - `MyTestAggregateChangedDescriptionEventMapper`
  - `MyTestAggregateDeletedEventMapper`
- an implementation of `ISerializationRegistry<T>` that provides quick access to the mappers in the `Repository<T>`
  - `MyTestAggregateSerializationRegistry`
- the base classes for the projections (implementing `AbstractProjection<T>` for each event)
  - `MyTestAggregateCreatedEventProjection`
  - `MyTestAggregateChangedNameEventProjection`
  - `MyTestAggregateChangedDescriptionEventProjection`
  - `MyTestAggregateDeletedEventProjection`
- an implementation of `IProjector<T>` that provides quick access to the projections in the `Repository<T>`
  - `MyTestAggregateProjector`
- an implementation of `IAggregator<T>` that provides quick access to the `Create` and `Apply` of the aggregate methods in the `Repository<T>`
  - `MyTestAggregateAggregator`
- A lot of `DependencyInjection` extensions to register the repository, serialization registry, aggregator, and projector in the dependency injection container.  
  The most important on is the `AddEventSourcing()` extension method, which will register **all** of them necessary services for event sourcing in the dependency injection container.

All those dependencies together will be provided to the `MyTestAggregateRepository` constructor, which looks like this:

```csharp
public partial class MyTestAggregateRepository : Repository<MyTestAggregate>
{
    public MyTestAggregateRepository(IEventStore eventStore, ISerializationRegistry<MyTestAggregate> serializationRegistry, IAggregator<MyTestAggregate> aggregator, IEnumerable<IProjector<MyTestAggregate>> projectors) : base(eventStore, serializationRegistry, aggregator, projectors) { }
}
```

**Remarks:**
- The `IEventStore` is an interface that represents the event store where the events are stored.  
  The event store is implemented in the `EventSourcing` library using the `EntityFrameworkCore` package.  
  For testing purposes, you can therefore use the `ImMemoryDatabase` provider of `EntityFrameworkCore` to store the events in memory.  
  For production, you can use any other (relational) database provider supported by `EntityFrameworkCore`, such as `SqlServer`, `PostgreSQL`, etc.
- There is a `SerializationRegistry<T>` implementation that uses reflection to find all the events and mappers belonging to an aggregate type.  
  For production, you should use the `EventSourcing.SourceGenerators` package to generate the serialization registry and mappers automatically.
- The generated projections must override the `ProjectAsync` method to provide the logic for projecting the events into read models.  
  Since the projections all inherit from the `AbstractProjection<T>` class, the default implementation of the `ProjectAsync` method will throw a `NotImplementedException` (on purpose) to ensure that you implement the projection logic in your projections.
- The different projections are used through the `IProjector<T>` interface, which provides a way to access the projections for an aggregate type.  
  One projector will be generated for each aggregate type by default, and it will provide access to all the projections for that aggregate type.  
  You can further implement your own projector to provide custom logic for projecting the events into read models.  
  Multiple projectors can be registered for the same aggregate type, and they will be executed in the order they are registered. (You can see that in the `IEnumerable<IProjector<MyTestAggregate>> projectors` parameter of the `MyTestAggregateRepository` constructor.)

### Projections

Projections are used to create read models from the events. They allow you to project the state of an aggregate into a format that is suitable for querying and displaying in the application.

The source generator will have generated the base projections for each event type, which you **must** then override to provide the projection logic.

Example of how to override a projection:

```csharp
public partial class MyTestAggregateCreatedEventProjection
{
    private readonly ILogger<MyTestAggregateCreatedEventProjection> _logger;

    public MyTestAggregateCreatedEventProjection(ILogger<MyTestAggregateCreatedEventProjection> logger)
    {
        _logger = logger;
    }
    public override Task ProjectAsync(MyTestAggregate state, CreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Projecting CreatedEvent - State: {State}", state);
        return Task.CompletedTask;
    }
}
```

## API Layer

In the API layer, you can expose the commands and queries to the clients. This layer will typically use ASP.NET Core or any other web framework to create RESTful APIs or gRPC services.

Implementation details will depend on the specific framework you are using, but the general idea is to create controllers or "minimal api" endpoints that handle the requests and use the mediator to send the commands to the application layer.

Examples of how to implement the API layer are not provided here, as they are out of the scope of this documentation.

### Dependency Injection

To register the repository and its dependencies in the dependency injection container, you can use the `AddEventSourcing` extension method provided by the source generator. 
This method will register all the necessary services for event sourcing, including the repository, serialization registry, aggregator, and projector.

The only other thing you need to do is to register the `IEventStore` implementation in the dependency injection container. 
With `EntityFrameworkCore`, you can use the `AddDbContext` method to register the `EventStoreDbContext`:

```csharp
using EventSourcing.Contexts;
using EventSourcing.Stores;
using MyTestProject.API.Common;
using MyTestProject.Application;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// ... reading configuration, setting up logging, etc. ...

builder.Services.AddDbContext<IEventStoreDbContext, EventStoreDbContext>(options => options
    .UseInMemoryDatabase("MyTestDatabase")
    .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))); // Ignore transaction warnings for in-memory database
builder.Services.AddScoped<IEventStore, EventStore>();
builder.Services.AddEventSourcing();builder.Services.AddEventSourcing();

// ... configure swagger, other services etc. ...

var app = builder.Build();

// ... configure middleware, authentication, authorization etc. ...

app.MapPost("/my-test", async (IMediator mediator, CreateCommand createCommand) => await mediator.Send(createCommand).ToWebResult())
    .Produces<Guid>(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
    .WithTags("MyTest")
    .WithName("MyTest_Create")
    .WithDescription("Creates a new MyTest object with the specified name and description.");

app.MapPatch("/my-test/name", async (IMediator mediator, ChangeNameCommand changeNameCommand) => await mediator.Send(changeNameCommand).ToWebResult())
    .Produces(StatusCodes.Status200OK)
    .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
    .WithTags("MyTest")
    .WithName("MyTest_ChangeName")
    .WithDescription("Changes the name of an existing MyTest object identified by its aggregate ID.");

// ... more endpoints for other commands, queries, etc. ...

app.Run();
```

This will register the `EventStore` as the implementation of the `IEventStore` interface, and it will also register the `EventStoreDbContext` as the implementation of the `IEventStoreDbContext` interface.

**Remarks:**
- Please be aware that the `/my-test/name` endpoint is just an example and that is not a good practice to have `/name` and/or `/description` as endpoints.  
  Not even the `ChangedNameEvent` and `ChangedDescriptionEvent` events are a specifically "good" practice, since you'd typically want to have a single `UpdateCommand` and `ChangedEvent` for this kind of (meta) data.    
  The `ChangedEvent` could then contain all the properties (e.g., as nullable types) that have changed, and you would apply it to the aggregate.  
  You'd have events only for significant state changes, such as `CreatedEvent`, `DeletedEvent` or `{Process}StartedEvent`/`{Process}FinishedEvent` to indicate an important change in the state of the aggregate (e.g., that a process has started or finished, etc.).  
  You're free to implement your own conventions for events and commands, but the general idea is to keep them meaningful and focused on significant state changes of the domain object rather than every little change in the aggregate's properties.
- The `ToWebResult()` extension method is a custom extension method that converts the `Result<T>`/`Result` (FluentResults) of the command handler into a web-friendly result, returning either the value or an error response.    
  The implementation of this method is not provided here (it is in the `MyTestProject.API.Common` namespace), but it is a good practice to have such an extension method to handle the conversion of results to HTTP responses consistently.