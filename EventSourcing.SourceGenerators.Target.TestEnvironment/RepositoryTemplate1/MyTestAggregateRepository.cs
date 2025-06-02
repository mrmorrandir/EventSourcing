using EventSourcing.Mappers;
using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.Events;
using EventSourcing.SourceGenerators.Target.TestEnvironment.Base;
using EventSourcing.Stores;
using FluentResults;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate1;

public class MyTestAggregateRepository : IMyAggregateRepository
{
    private static readonly CreatedEventMapper _eventSourcingSourceGeneratorsTargetDomainEventsCreatedEventMapper = new();
    private static readonly ChangedNameEventMapper _eventSourcingSourceGeneratorsTargetDomainEventsChangedNameEventMapper = new();
    private static readonly ChangedDescriptionEventMapper _eventSourcingSourceGeneratorsTargetDomainEventsChangedDescriptionEventMapper = new();
    private static readonly DeletedEventMapper _eventSourcingSourceGeneratorsTargetDomainEventsDeletedEventMapper = new();
    private static readonly Dictionary<string, Func<string, string, IEvent>> _deserializers = new();
    private readonly IEventStore _eventStore;
    private readonly Dictionary<Guid, int> _expectedVersions = new();

    static MyTestAggregateRepository()
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

    public MyTestAggregateRepository(IEventStore eventStore) => _eventStore = eventStore;

    public async Task<Result<Aggregate<MyTestAggregate>>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        List<IEventEntity>? eventDataList;
        try
        {
            //TODO: Hier muss ncoh was gemacht werden....
            var eventData = await _eventStore.GetAsync(id, cancellationToken);
            eventDataList = eventData.ToList();
            if (_expectedVersions.ContainsKey(id))
                _expectedVersions[id] = eventDataList.Max(x => x.Version);
            else
                _expectedVersions.Add(id, eventDataList.Max(x => x.Version));
        }
        catch (Exception ex)
        {
            return new Error($"Error while getting events for aggregate with id '{id}'. #GetEventsFailed").CausedBy(ex);
        }

        var eventInstances = new List<IEvent>();
        foreach (var evt in eventDataList)
            try
            {
                var eventInstance = Deserialize(evt.Schema, evt.Data);
                eventInstances.Add(eventInstance);
            }
            catch (Exception ex)
            {
                return new Error($"Error while deserializing event with schema '{evt.Schema}' from stream with id '{id}'. #DeserializeEventFailed").CausedBy(ex);
            }

        if (!eventInstances.Any())
            return new Error($"No events found for aggregate with id '{id}'. #NoEventsFound");

        var createResult = CreateAggregateFromEvents(eventInstances);
        if (createResult.IsFailed)
            return createResult.ToResult();
        
        var aggregate = createResult.Value;
        var version = eventDataList.Max(x => x.Version);
        
        return new Aggregate<MyTestAggregate>(aggregate, version);
    }

    public async Task<Result> SaveAsync(Guid id, IEnumerable<IEvent> events, int expectedVersion, CancellationToken cancellationToken)
    {
        var serializedEvents = new List<EventEntity>();
        try
        {
            foreach (var evt in events)
            {
                var serializedEvent = Serialize(evt);
                var eventData = new EventEntity
                {
                    Id = Guid.NewGuid(),
                    Created = DateTimeOffset.Now,
                    StreamId = id,
                    Version = ++expectedVersion,
                    Schema = serializedEvent.Schema,
                    Data = serializedEvent.Data
                };
                serializedEvents.Add(eventData);
            }
        }
        catch (Exception ex)
        {
            return new Error($"Error while serializing events for aggregate with id '{id}'. #SerializeEventsFailed").CausedBy(ex);
        }

        try
        {
            await _eventStore.AppendAsync(id, expectedVersion, serializedEvents, cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return new Error($"Error while appending events to aggregate with id '{id}'. #AppendEventsFailed").CausedBy(ex);
        }

        // Lokales Mapping durchführen damit die Daten nicht neu geladen werden müssen.
        //var aggregateResult = ApplyEvents(new MyTestAggregate(), serializedEvents.Select(e => Deserialize(e.Schema, e.Data)).ToList());
    }

    // Methode umbenennen in SaveAndRefreshAsync... damit klar wird, dass sie die Daten neu läd.
    public async Task<Result<Aggregate<MyTestAggregate>>> SaveAndUpdateAsync(Guid id, IEnumerable<IEvent> events, int expectedVersion, CancellationToken cancellationToken)
    {
        var result = SaveAsync(id, events, expectedVersion, cancellationToken).GetAwaiter().GetResult();
        if (result.IsFailed)
            return result;

        return await GetAsync(id, cancellationToken);
    }

    private Result<MyTestAggregate> CreateAggregateFromEvents(List<IEvent> events)
    {
        if (events.Count == 0)
            return new Error("No events provided to create aggregate. #NoEventsProvided");

        MyTestAggregate? aggregate = null;
        try
        {
            foreach (var evt in events)
                aggregate = aggregate == null ? CreateFromEvent(evt) : ApplyEvent(aggregate, evt);

            if (aggregate == null)
                return new Error("Aggregate could not be created from provided events. #AggregateCreationFailed");

            return aggregate;
        }
        catch (Exception ex)
        {
            return new Error("Error while applying events to create aggregate. #ApplyEventsFailed").CausedBy(ex);
        }
    }
    
    private Result<MyTestAggregate> ApplyEvents(MyTestAggregate aggregate, List<IEvent> events)
    {
        if (events.Count == 0)
            return new Error("No events provided to create aggregate. #NoEventsProvided");
        try
        {
            foreach (var evt in events)
                aggregate = ApplyEvent(aggregate, evt);

            return aggregate;
        }
        catch (Exception ex)
        {
            return new Error("Error while applying events to create aggregate. #ApplyEventsFailed").CausedBy(ex);
        }
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

    private static ISerializedEvent Serialize(IEvent @event)
    {
        return @event.GetType() switch
        {
            { } type when type == typeof(CreatedEvent) => _eventSourcingSourceGeneratorsTargetDomainEventsCreatedEventMapper.Serialize((CreatedEvent)@event),
            { } type when type == typeof(ChangedNameEvent) => _eventSourcingSourceGeneratorsTargetDomainEventsChangedNameEventMapper.Serialize((ChangedNameEvent)@event),
            { } type when type == typeof(ChangedDescriptionEvent) => _eventSourcingSourceGeneratorsTargetDomainEventsChangedDescriptionEventMapper.Serialize((ChangedDescriptionEvent)@event),
            { } type when type == typeof(DeletedEvent) => _eventSourcingSourceGeneratorsTargetDomainEventsDeletedEventMapper.Serialize((DeletedEvent)@event),
            _ => throw new EventRegistryException($"No serializer found for type {@event.GetType().Name}")
        };
    }

    private static IEvent Deserialize(string schema, string data)
    {
        if (!_deserializers.TryGetValue(schema, out var deserializer))
            throw new EventRegistryException($"No deserializer found for type {schema}");

        return deserializer(schema, data);
    }
}