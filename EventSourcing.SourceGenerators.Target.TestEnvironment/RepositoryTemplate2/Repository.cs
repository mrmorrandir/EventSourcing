using EventSourcing.SourceGenerators.Target.Domain.Events;
using EventSourcing.Stores;
using FluentResults;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

public class Repository<TAggregate> : IRepository<TAggregate> where TAggregate : IAggregate
{
    private readonly IEventStoreX _eventStore;
    private readonly ISerializationRegistry<TAggregate> _serializationRegistry;
    private readonly IAggregator<TAggregate> _aggregator;

    public Repository(IEventStoreX eventStore, ISerializationRegistry<TAggregate> serializationRegistry, IAggregator<TAggregate> aggregator)
    {
        _eventStore = eventStore;
        _serializationRegistry = serializationRegistry;
        _aggregator = aggregator;
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
        
        // Create the aggregate from the events in the stream
        var aggregateResult = CreateAggregateFromEvents(eventStream.Events);
        if (aggregateResult.IsFailed)
            return aggregateResult.ToResult();
        
        var aggregate = aggregateResult.Value;
        
        // Execute the update function to get the new events
        var updateResult = await Result.Try(() => update(aggregate));
        if (updateResult.IsFailed)
            return new Error("Failed to update aggregate").CausedBy(updateResult.Errors);

        // Check if there are any new events to append
        var events = updateResult.Value.ToList();
        if (events.Count == 0)
            return Result.Ok(aggregate).WithSuccess("No changes made to the aggregate, nothing to save.");

        // Append the new events to the event stream
        var expectedVersion = eventStream.BaseVersion;
        foreach (var evt in events)
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
        
        // Save the event stream
        var saveResult = await eventStream.SaveAsync(cancellationToken);
        if (saveResult.IsFailed)
            return new Error("Failed to save event stream").CausedBy(saveResult.Errors);

        var updateAggregateResult = CreateAggregateFromEvents(eventStream.Events);
        if (updateAggregateResult.IsFailed)
            return new Error("Failed to update aggregate from events").CausedBy(updateAggregateResult.Errors);
        
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
        
        var createAggregateResult = CreateAggregateFromEvents(eventStream.Events);
        if (createAggregateResult.IsFailed)
            return new Error("Failed to create aggregate from events").CausedBy(createAggregateResult.Errors);

        return createAggregateResult.Value;
    }
    
    private Result<TAggregate> CreateAggregateFromEvents(IEnumerable<EventEntity> events)
    {
        TAggregate? aggregate = default;
        foreach (var eventEntity in events)
        {
            var deserializeResult = _serializationRegistry.Deserialize(eventEntity.Schema, eventEntity.Data);
            if (deserializeResult.IsFailed)
                return new Error("Failed to deserialize event").CausedBy(deserializeResult.Errors);

            var @event = deserializeResult.Value;
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