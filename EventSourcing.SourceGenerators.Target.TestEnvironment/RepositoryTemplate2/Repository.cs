using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.Events;
using EventSourcing.Stores;
using FluentResults;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

// TODO: I must add the IEventBus to the repository so that it can publish events after they are saved. Alternatively, I can create something like an IEventPublisher or IEventProjector - maybe as base-class/partial-class (with just logging message) to be overwritten...
// TODO: Maybe I can couple the EventStoreDbContext with another context with the help of transactions, so that I can make sure that the events are saved and their projections, too - otherwise get rid of both (rollback)?!?
/// <summary>
/// This is a generic repository for aggregates.
/// <param>
/// 
/// </param>
/// </summary>
/// <typeparam name="TAggregate"></typeparam>
public class Repository<TAggregate> : IRepository<TAggregate> where TAggregate : IAggregate
{
    private readonly IEventStoreX _eventStore;
    private readonly ISerializationRegistry<TAggregate> _serializationRegistry;
    private readonly IAggregator<TAggregate> _aggregator;
    private readonly IEnumerable<IProjector<TAggregate>> _projectors;

    public Repository(IEventStoreX eventStore, ISerializationRegistry<TAggregate> serializationRegistry, IAggregator<TAggregate> aggregator, IEnumerable<IProjector<TAggregate>> projectors)
    {
        _eventStore = eventStore;
        _serializationRegistry = serializationRegistry;
        _aggregator = aggregator;
        _projectors = projectors;
    }

    public async Task<Result<TAggregate>> CreateAsync(Func<Task<CreatedEvent>> create, CancellationToken cancellationToken = default)
    {
        var createResult = await Result.Try(create);
        if (createResult.IsFailed)
            return new Error("Failed to create aggregate").CausedBy(createResult.Errors);

        return await CreateAsync(createResult.Value, cancellationToken);
    }
    
    public async Task<Result<TAggregate>> CreateAsync(Func<CreatedEvent> create, CancellationToken cancellationToken)
    {
        return await CreateAsync(() => Task.FromResult(create()), cancellationToken);
    }

    public async Task<Result<TAggregate>> UpdateAsync(Guid aggregateId, Func<TAggregate, Task<List<IEvent>>> update, CancellationToken cancellationToken = default)
    {
        // Get the event stream for the aggregate
        var getResult = await _eventStore.GetStreamAsync(aggregateId, cancellationToken);
        if (getResult.IsFailed)
            return new Error("Failed to get event stream").CausedBy(getResult.Errors);
        
        var eventStream = getResult.Value;
        
        var eventsResult = CreateEventsFromEntities(eventStream.Events);
        if (eventsResult.IsFailed)
            return new Error("Failed to create events from event entities").CausedBy(eventsResult.Errors);
        
        // Create the aggregate from the events in the stream
        var aggregateResult = CreateAggregateFromEvents(eventsResult.Value);
        if (aggregateResult.IsFailed)
            return aggregateResult.ToResult();
        
        var aggregate = aggregateResult.Value;
        
        // Execute the update function to get the new events
        var updateResult = await Result.Try(() => update(aggregate));
        if (updateResult.IsFailed)
            return new Error("Failed to update aggregate").CausedBy(updateResult.Errors);

        // Check if there are any new events to append
        var newEvents = updateResult.Value.ToList();
        if (newEvents.Count == 0)
            return Result.Ok(aggregate).WithSuccess("No changes made to the aggregate, nothing to save.");

        // Append the new events to the event stream
        var expectedVersion = eventStream.BaseVersion;
        foreach (var evt in newEvents)
        {
            var serializeResult = _serializationRegistry.Serialize(evt);
            if (serializeResult.IsFailed)
                return new Error($"Failed to serialize event of type {evt.GetType().Name}").CausedBy(serializeResult.Errors);

            var serializedEvent = serializeResult.Value;
            var eventEntity = new EventEntity(evt.AggregateId, ++expectedVersion, serializedEvent.Schema, serializedEvent.Data);
            var appendResult = await eventStream.AppendAsync(eventEntity, cancellationToken);
            if (appendResult.IsFailed)
                return new Error("Failed to append event to stream").CausedBy(appendResult.Errors);
        }

        var eventsResultUpdated = CreateEventsFromEntities(eventStream.Events);
        if (eventsResultUpdated.IsFailed)
            return new Error("Failed to create events from event entities").CausedBy(eventsResultUpdated.Errors);
        
        var updateAggregateResult = CreateAggregateFromEvents(eventsResultUpdated.Value);
        if (updateAggregateResult.IsFailed)
            return new Error("Failed to update aggregate from events").CausedBy(updateAggregateResult.Errors);
        
        // Project the new events
        foreach (var projector in _projectors)
        {
            foreach (var evt in newEvents)
            {
                var projectionResult = await projector.ProjectAsync(aggregate, evt, cancellationToken);
                if (projectionResult.IsFailed)
                    return new Error($"Failed to project event of type {evt.GetType().Name}").CausedBy(projectionResult.Errors);
            }
        }
        
        // TODO: Should I save the events only when there are no errors in the projections? Or should I save them anyway and just log the errors?
        // I could leave it like it is, so that the events are not saved if there are errors in the projections - but this way I can decide in the projection if it is important enough to not save the events.
        
        // Save the event stream
        var saveResult = await eventStream.SaveAsync(cancellationToken);
        if (saveResult.IsFailed)
            return new Error("Failed to save event stream").CausedBy(saveResult.Errors);
        
        return Result.Ok(updateAggregateResult.Value);
    }

    public async Task<Result<TAggregate>> UpdateAsync(Guid aggregateId, Func<TAggregate, List<IEvent>> update, CancellationToken cancellationToken = default)
    {
        return await UpdateAsync(aggregateId, aggregate => Task.FromResult(update(aggregate)), cancellationToken);
    }
    
    private async Task<Result<TAggregate>> CreateAsync(IEvent createdEvent, CancellationToken cancellationToken = default)
    {
        var eventStreamResult = await _eventStore.CreateStreamAsync(createdEvent.AggregateId, cancellationToken);
        if (eventStreamResult.IsFailed)
            return new Error("Failed to create event stream").CausedBy(eventStreamResult.Errors);
        
        var eventStream = eventStreamResult.Value;
        
        var serializeResult = _serializationRegistry.Serialize(createdEvent);
        if (serializeResult.IsFailed)
            return new Error($"Failed to serialize event of type {createdEvent.GetType().Name}").CausedBy(serializeResult.Errors);
        
        var serializedEvent = serializeResult.Value;
        var eventData = new EventEntity(createdEvent.AggregateId, 1, serializedEvent.Schema, serializedEvent.Data);
        
        var appendResult = await eventStream.AppendAsync(eventData, cancellationToken);
        if (appendResult.IsFailed)
            return new Error("Failed to append event to stream").CausedBy(appendResult.Errors);
        
        var saveResult = await eventStream.SaveAsync(cancellationToken);
        if (saveResult.IsFailed)
            return new Error("Failed to save event stream").CausedBy(saveResult.Errors);

        var eventsResult = CreateEventsFromEntities(eventStream.Events);
        if (eventsResult.IsFailed)
            return new Error("Failed to create events from event entities").CausedBy(eventsResult.Errors);
        
        var createAggregateResult = CreateAggregateFromEvents(eventsResult.Value);
        if (createAggregateResult.IsFailed)
            return new Error("Failed to create aggregate from events").CausedBy(createAggregateResult.Errors);

        return createAggregateResult.Value;
    }

    private Result<List<IEvent>> CreateEventsFromEntities(IEnumerable<EventEntity> eventEntities)
    {
        var events = new List<IEvent>();
        foreach (var eventEntity in eventEntities)
        {
            var deserializeResult = _serializationRegistry.Deserialize(eventEntity.Schema, eventEntity.Data);
            if (deserializeResult.IsFailed)
                return new Error("Failed to deserialize event").CausedBy(deserializeResult.Errors);

            var @event = deserializeResult.Value;
            events.Add(@event);
        }

        return events;
    }
    
    private Result<TAggregate> CreateAggregateFromEvents(IEnumerable<IEvent> events)
    {
        TAggregate? aggregate = default;
        foreach (var @event in events)
        {
            if (aggregate == null)
            {
                var createResult = Result.Try(() => _aggregator.CreateFromEvent(@event));
                if (createResult.IsFailed)
                    return new Error($"Failed to create aggregate from event of type {@event.GetType().Name}").CausedBy(createResult.Errors);
                aggregate = createResult.Value;
                continue;
            }
            // ReSharper disable once AccessToModifiedClosure
            var applyResult = Result.Try(() => _aggregator.ApplyEvent(aggregate, @event));
            if (applyResult.IsFailed)
                return new Error($"Failed to apply event of type {@event.GetType().Name} to aggregate").CausedBy(applyResult.Errors);
            aggregate = applyResult.Value;
        }

        return aggregate == null ? new Error("Aggregate could not be created from events") : Result.Ok(aggregate);
    }
}

public interface IProjector<in TAggregate> where TAggregate : IAggregate
{
    //TODO: Return a transaction object or something similar to save the state/model of the projection in a transaction if needed.
    /// <summary>
    /// This method projects the events to a new state or model.
    /// <para>
    /// When the event returns a Result.Fail, the events of the latest action are not saved.
    /// </para>
    /// </summary>
    /// <param name="state"></param>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Result> ProjectAsync(TAggregate state, IEvent @event, CancellationToken cancellationToken = default);
}


/// <summary>
/// This has to be source generated
/// </summary>
/// <typeparam name="TAggregate"></typeparam>
public class MyTestAggregateProjector : IProjector<MyTestAggregate> 
{
    private readonly CreatedEventProjection _createdEventProjection;
    private readonly ChangedNameEventProjection _changedNameEventProjection;
    private readonly ChangedDescriptionEventProjection _changedDescriptionEventProjection;

    /// <summary>
    /// Events must be registered
    /// </summary>
    /// <param name="createdEventProjection"></param>
    /// <param name="changedNameEventProjection"></param>
    public MyTestAggregateProjector(CreatedEventProjection createdEventProjection, ChangedNameEventProjection changedNameEventProjection, ChangedDescriptionEventProjection changedDescriptionEventProjection)
    {
        _createdEventProjection = createdEventProjection;
        _changedNameEventProjection = changedNameEventProjection;
        _changedDescriptionEventProjection = changedDescriptionEventProjection;
    }
    
    public async Task<Result> ProjectAsync(MyTestAggregate state, IEvent @event, CancellationToken cancellationToken = default)
    {
        return @event.GetType() switch
        {
            {  } type when type == typeof(CreatedEvent) => await Result.Try(() => _createdEventProjection.ProjectAsync(state, (CreatedEvent)@event, cancellationToken)),
            {  } type when type == typeof(ChangedNameEvent) => await Result.Try(() => _changedNameEventProjection.ProjectAsync(state, (ChangedNameEvent)@event, cancellationToken)),
            {  } type when type == typeof(ChangedDescriptionEvent) => await Result.Try(() => _changedDescriptionEventProjection.ProjectAsync(state, (ChangedDescriptionEvent)@event, cancellationToken)),
            _ => Result.Fail($"Projection for event '{@event.GetType().Name}' of '{state.GetType().Name}' was not found.")
        };
        
        // TODO: Return some Transaction-Instance so that I can save the state/model of the projection in a transaction if needed!
    }
}

public interface IProjection<TAggregate, TEvent> where TAggregate : IAggregate where TEvent : IEvent
{
    Task ProjectAsync(TAggregate state, TEvent @event, CancellationToken cancellationToken = default);
}


/// <summary>
/// This has to be source generated (without constructor)
/// </summary>
public partial class CreatedEventProjection : AbstractProjection<MyTestAggregate, CreatedEvent>
{
    
}

/// <summary>
/// This has to be manually implemented
/// </summary>
public partial class CreatedEventProjection
{
    private readonly ILogger<CreatedEventProjection> _logger;

    public CreatedEventProjection(ILogger<CreatedEventProjection> logger)
    {
        _logger = logger;
    }
    
    public override Task ProjectAsync(MyTestAggregate state, CreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Projecting CreatedEvent for aggregate {AggregateId}", state.Id);
        return Task.CompletedTask;
    }
}

/// <summary>
/// This has to be source generated (without constructor)
/// </summary>
public partial class ChangedNameEventProjection : AbstractProjection<MyTestAggregate, ChangedNameEvent>
{

}

/// <summary>
/// This has to be manually implemented
/// </summary>
public partial class ChangedNameEventProjection
{
    // This is a manually implemented projection for the ChangedNameEvent.
    // It can be used to log or perform additional actions when the name of the aggregate changes.

    private readonly ILogger<ChangedNameEventProjection> _logger;

    public ChangedNameEventProjection(ILogger<ChangedNameEventProjection> logger)
    {
        _logger = logger;
    }

    public override Task ProjectAsync(MyTestAggregate state, ChangedNameEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Projecting ChangedNameEvent for aggregate {AggregateId}", state.Id);
        return Task.CompletedTask;
    }
}

public partial class ChangedDescriptionEventProjection : AbstractProjection<MyTestAggregate, ChangedDescriptionEvent>
{
    
}

/// <summary>
/// This has to be manually implemented
/// </summary>
public partial class ChangedDescriptionEventProjection
{
    // This is a manually implemented projection for the ChangedNameEvent.
    // It can be used to log or perform additional actions when the name of the aggregate changes.

    private readonly ILogger<ChangedDescriptionEventProjection> _logger;

    public ChangedDescriptionEventProjection(ILogger<ChangedDescriptionEventProjection> logger)
    {
        _logger = logger;
    }

    public override Task ProjectAsync(MyTestAggregate state, ChangedDescriptionEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Projecting ChangedDescriptionEvent for aggregate {AggregateId}", state.Id);
        return Task.CompletedTask;
    }
}




public abstract class AbstractProjection<TAggregate, TEvent> : IProjection<TAggregate, TEvent> where TAggregate : IAggregate where TEvent : IEvent
{
    public virtual Task ProjectAsync(TAggregate state, TEvent @event, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException($"Projection for event '{@event.GetType().Name}' of '{state.GetType().Name}' is not implemented.");
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddProjections(this IServiceCollection services)
    {
        // Register the projections
        services.AddScoped<CreatedEventProjection>();
        services.AddScoped<ChangedNameEventProjection>();
        services.AddScoped<ChangedDescriptionEventProjection>();

        // Register the projector
        services.AddScoped<IProjector<MyTestAggregate>, MyTestAggregateProjector>();

        return services;
    }
}