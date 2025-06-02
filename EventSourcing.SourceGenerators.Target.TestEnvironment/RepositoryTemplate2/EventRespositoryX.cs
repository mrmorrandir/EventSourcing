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
    public async Task<Result> CreateAsync(Func<Task<Result<CreatedEvent>>> create, CancellationToken cancellationToken)
    {
        var createResult = await create();
        if (createResult.IsFailed)
            return new Error("Failed to create aggregate").CausedBy(createResult.Errors);

        return await CreateAsync(createResult.Value, cancellationToken);
    }
    
    public async Task<Result> CreateAsync(Func<CreatedEvent> create, CancellationToken cancellationToken)
    {
        var createResult = Result.Try(create);
        if (createResult.IsFailed)
            return new Error("Failed to create aggregate").CausedBy(createResult.Errors);
        
        var createdEvent = createResult.Value;
        
        return await CreateAsync(createdEvent, cancellationToken);
    }

    private async Task<Result> CreateAsync(IEvent createdEvent, CancellationToken cancellationToken)
    {
        var eventStreamResult = await _eventStore.CreateAsync(createdEvent.Id, cancellationToken);
        if (eventStreamResult.IsFailed)
            return new Error("Failed to create event stream").CausedBy(eventStreamResult.Errors);
        
        var eventStream = eventStreamResult.Value;
        
        var serializeResult = Serialize(createdEvent);
        if (serializeResult.IsFailed)
            return new Error($"Failed to serialize event of type {createdEvent.GetType().Name}").CausedBy(serializeResult.Errors);
        
        var serializedEvent = serializeResult.Value;
        var eventData = new EventEntity(createdEvent.Id, 1, serializedEvent.Schema, serializedEvent.Data);
        
        var appendResult = await eventStream.AppendAsync(eventData, cancellationToken);
        if (appendResult.IsFailed)
            return new Error("Failed to append event to stream").CausedBy(appendResult.Errors);
        
        return await eventStream.SaveAsync(cancellationToken);
    }

    public async Task<Result<MyTestAggregate>> UpdateAsync(Guid id, Func<MyTestAggregate, Task<Result<List<IEvent>>>> update, CancellationToken cancellationToken)
    {
        // Get the event stream for the aggregate
        var getResult = await _eventStore.GetAsync(id, cancellationToken);
        if (getResult.IsFailed)
            return new Error("Failed to get event stream").CausedBy(getResult.Errors);
        
        var eventStream = getResult.Value;
        
        // Create the aggregate from the events in the stream
        var aggregateResult = CreateAndApplyEvents(eventStream.Events);
        if (aggregateResult.IsFailed)
            return aggregateResult.ToResult();
        
        var aggregate = aggregateResult.Value;
        
        // Execute the update function to get the new events
        var updateResult = await update(aggregate);
        if (updateResult.IsFailed)
            return new Error("Failed to update aggregate").CausedBy(updateResult.Errors);

        // Validate the events returned by the update function
        var events = updateResult.Value.ToList();
        if (!events.Any())
            return Result.Ok().WithSuccess("No changes made to the aggregate, nothing to save.");
        
        if (events.Any(x => x.Id != id))
            return new Error("All events must have the same Id as the aggregate").CausedBy("IdMismatch");

        // Append the new events to the event stream
        var expectedVersion = eventStream.BaseVersion;
        foreach (var evt in events)
        {
            var serializeResult = Serialize(evt);
            if (serializeResult.IsFailed)
                return new Error($"Failed to serialize event of type {evt.GetType().Name}").CausedBy(serializeResult.Errors);

            var serializedEvent = serializeResult.Value;
            var eventEntity = new EventEntity(evt.Id, ++expectedVersion, serializedEvent.Schema, serializedEvent.Data);
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
    
    private Result<MyTestAggregate> CreateAndApplyEvents(IEnumerable<EventEntity> events)
    {
        MyTestAggregate? aggregate = null;
        foreach (var eventEntity in events)
        {
            var deserializeResult = Result.Try(() => Deserialize(eventEntity.Schema, eventEntity.Data));
            if (deserializeResult.IsFailed)
                return new Error("Failed to deserialize event").CausedBy(deserializeResult.Errors);
            
            var @event = deserializeResult.Value;
            try
            {
                aggregate = aggregate == null ? CreateFromEvent(@event) : ApplyEvent(aggregate, @event);
            } catch (Exception ex)
            {
                return new Error($"Failed to apply event of type {@event.GetType().Name}").CausedBy(ex);
            }
        }
        
        if (aggregate == null)
            return new Error("Aggregate could not be created from events");
        
        return Result.Ok(aggregate);
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