using EventSourcing.Mappers;
using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.Events;
using EventSourcing.Stores;
using FluentResults;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories;

public class MyTestAggregateRepositoryX
{
    private static readonly CreatedEventMapper _eventSourcingSourceGeneratorsTargetDomainEventsCreatedEventMapper = new();
    private static readonly ChangedNameEventMapper _eventSourcingSourceGeneratorsTargetDomainEventsChangedNameEventMapper = new();
    private static readonly ChangedDescriptionEventMapper _eventSourcingSourceGeneratorsTargetDomainEventsChangedDescriptionEventMapper = new();
    private static readonly DeletedEventMapper _eventSourcingSourceGeneratorsTargetDomainEventsDeletedEventMapper = new();
    private static readonly Dictionary<string, Func<string, string, IEvent>> _deserializers = new();
    private readonly EventStoreX _eventStore;

    static MyTestAggregateRepositoryX()
    {
        foreach (var schema in _eventSourcingSourceGeneratorsTargetDomainEventsCreatedEventMapper.Schemas)
            _deserializers.Add(schema, (typeSchema, data) => _eventSourcingSourceGeneratorsTargetDomainEventsCreatedEventMapper.Deserialize(typeSchema, data));
        foreach (var schema in _eventSourcingSourceGeneratorsTargetDomainEventsChangedNameEventMapper.Schemas)
            _deserializers.Add(schema, (typeSchema, data) => _eventSourcingSourceGeneratorsTargetDomainEventsChangedNameEventMapper.Deserialize(typeSchema, data));
        foreach (var schema in _eventSourcingSourceGeneratorsTargetDomainEventsChangedDescriptionEventMapper.Schemas)
            _deserializers.Add(schema, (typeSchema, data) => _eventSourcingSourceGeneratorsTargetDomainEventsChangedDescriptionEventMapper.Deserialize(typeSchema, data));
        foreach (var schema in _eventSourcingSourceGeneratorsTargetDomainEventsDeletedEventMapper.Schemas)
            _deserializers.Add(schema, (typeSchema, data) => _eventSourcingSourceGeneratorsTargetDomainEventsDeletedEventMapper.Deserialize(typeSchema, data));
    }

    public MyTestAggregateRepositoryX(EventStoreX eventStore) => _eventStore = eventStore;


    // This methods have to be created for each "Create" Method found in the aggregate.
    public async Task<Result<MyTestAggregate>> CreateAsync(Func<Task<CreatedEvent>> create, CancellationToken cancellationToken)
    {
        var createResult = await Result.Try(create);
        if (createResult.IsFailed)
            return new Error("Failed to create aggregate").CausedBy(createResult.Errors);

        return await CreateAsync(createResult.Value, cancellationToken);
    }
    
    public async Task<Result<MyTestAggregate>> CreateAsync(Func<CreatedEvent> create, CancellationToken cancellationToken)
    {
        return await CreateAsync(() => Task.FromResult(create()), cancellationToken);
    }

    public async Task<Result<MyTestAggregate>> UpdateAsync(Guid aggregateId, Func<MyTestAggregate, Task<List<IEvent>>> update, CancellationToken cancellationToken)
    {
        // Get the event stream for the aggregate
        var getResult = await _eventStore.GetAsync(aggregateId, cancellationToken);
        if (getResult.IsFailed)
            return new Error("Failed to get event stream").CausedBy(getResult.Errors);
        
        var eventStream = getResult.Value;
        
        // Create the aggregate from the events in the stream
        var aggregateResult = CreateAndApplyEvents(eventStream.Events);
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
            var serializeResult = Serialize(evt);
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

        var updateAggregateResult = CreateAndApplyEvents(eventStream.Events);
        if (updateAggregateResult.IsFailed)
            return new Error("Failed to update aggregate from events").CausedBy(updateAggregateResult.Errors);
        
        return Result.Ok(updateAggregateResult.Value);
    }

    public async Task<Result<MyTestAggregate>> UpdateAsync(Guid aggregateId, Func<MyTestAggregate, List<IEvent>> update, CancellationToken cancellationToken)
    {
        return await UpdateAsync(aggregateId, aggregate => Task.FromResult(update(aggregate)), cancellationToken);
    }
    
    private async Task<Result<MyTestAggregate>> CreateAsync(IEvent createdEvent, CancellationToken cancellationToken)
    {
        var eventStreamResult = await _eventStore.CreateAsync(createdEvent.AggregateId, cancellationToken);
        if (eventStreamResult.IsFailed)
            return new Error("Failed to create event stream").CausedBy(eventStreamResult.Errors);
        
        var eventStream = eventStreamResult.Value;
        
        var serializeResult = Serialize(createdEvent);
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
        
        var createAggregateResult = CreateAndApplyEvents(eventStream.Events);
        if (createAggregateResult.IsFailed)
            return new Error("Failed to create aggregate from events").CausedBy(createAggregateResult.Errors);

        return createAggregateResult.Value;
    }

    private Result<MyTestAggregate> CreateAndApplyEvents(IEnumerable<EventEntity> events)
    {
        MyTestAggregate? aggregate = null;
        foreach (var eventEntity in events)
        {
            var deserializeResult = Deserialize(eventEntity.Schema, eventEntity.Data);
            if (deserializeResult.IsFailed)
                return new Error("Failed to deserialize event").CausedBy(deserializeResult.Errors);
            
            var @event = deserializeResult.Value;
            if (aggregate is null)
            {
                var createResult = Result.Try(() => CreateFromEvent(@event));
                if (createResult.IsFailed)
                    return new Error($"Failed to create aggregate from event of type {@event.GetType().Name}").CausedBy(createResult.Errors);
                aggregate = createResult.Value;
                continue;
            }
            var applyResult = Result.Try(() => ApplyEvent(aggregate, @event));
            if (applyResult.IsFailed)
                return new Error($"Failed to apply event of type {@event.GetType().Name} to aggregate").CausedBy(applyResult.Errors);
            aggregate = applyResult.Value;
        }
        
        return aggregate is null ? new Error("Aggregate could not be created from events") : Result.Ok(aggregate);
    }

    private static MyTestAggregate ApplyEvent(MyTestAggregate aggregate, object evt)
    {
        return evt switch
        {
            ChangedNameEvent e => aggregate.Apply(e),
            ChangedDescriptionEvent e => aggregate.Apply(e),
            DeletedEvent e => aggregate.Apply(e),
            _ => throw new InvalidOperationException($"Unknown event type: {evt.GetType().Name}")
        };
    }

    private static MyTestAggregate CreateFromEvent(object evt)
    {
        return evt switch
        {
            CreatedEvent e => MyTestAggregate.Create(e),
            _ => throw new InvalidOperationException($"Unknown event type: {evt.GetType().Name}")
        };
    }

    private static Result<ISerializedEvent> Serialize(IEvent @event)
    {
        return @event.GetType() switch
        {
            { } type when type == typeof(CreatedEvent) => Result.Try(() => _eventSourcingSourceGeneratorsTargetDomainEventsCreatedEventMapper.Serialize((CreatedEvent)@event)),
            { } type when type == typeof(ChangedNameEvent) => Result.Try(() => _eventSourcingSourceGeneratorsTargetDomainEventsChangedNameEventMapper.Serialize((ChangedNameEvent)@event)),
            { } type when type == typeof(ChangedDescriptionEvent) => Result.Try(() => _eventSourcingSourceGeneratorsTargetDomainEventsChangedDescriptionEventMapper.Serialize((ChangedDescriptionEvent)@event)),
            { } type when type == typeof(DeletedEvent) => Result.Try(() => _eventSourcingSourceGeneratorsTargetDomainEventsDeletedEventMapper.Serialize((DeletedEvent)@event)),
            _ => Result.Fail($"No serializer found for type {@event.GetType().Name}")
        };
    }

    private static Result<IEvent> Deserialize(string schema, string data)
    {
        if (!_deserializers.TryGetValue(schema, out var deserializer))
            throw new EventRegistryException($"No deserializer found for type {schema}");

        return Result.Try(() => deserializer(schema, data));
    }
}