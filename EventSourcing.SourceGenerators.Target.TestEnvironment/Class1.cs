using EventSourcing.Mappers;
using EventSourcing.Stores;
using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.Events;
using FluentResults;
using System.Linq;


namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories;

public partial class CreatedEventMapper : AbstractEventMapper<CreatedEvent>
{
    public CreatedEventMapper()
    {
        WillSerialize("created-event-v1");
        CanDeserialize("created-event-v1");
        Configure();
    }

    partial void Configure();
}

public partial class ChangedNameEventMapper : AbstractEventMapper<ChangedNameEvent>
{
    public ChangedNameEventMapper()
    {
        WillSerialize("changed-name-event-v1");
        CanDeserialize("changed-name-event-v1");
        Configure();
    }

    partial void Configure();
}

public partial class ChangedDescriptionEventMapper : AbstractEventMapper<ChangedDescriptionEvent>
{
    public ChangedDescriptionEventMapper()
    {
        WillSerialize("changed-description-event-v1");
        CanDeserialize("changed-description-event-v1");
        Configure();
    }

    partial void Configure();
}

public partial class DeletedEventMapper : AbstractEventMapper<DeletedEvent>
{
    public DeletedEventMapper()
    {
        WillSerialize("deleted-event-v1");
        CanDeserialize("deleted-event-v1");
        Configure();
    }

    partial void Configure();
}

public record Aggregate<T>(T Instance, int Version) where T : IAggregate;

public class MyTestAggregateRepository
{
    private static readonly CreatedEventMapper _eventSourcingSourceGeneratorsTargetDomainEventsCreatedEventMapper = new();
    private static readonly ChangedNameEventMapper _eventSourcingSourceGeneratorsTargetDomainEventsChangedNameEventMapper = new();
    private static readonly ChangedDescriptionEventMapper _eventSourcingSourceGeneratorsTargetDomainEventsChangedDescriptionEventMapper = new();
    private static readonly DeletedEventMapper _eventSourcingSourceGeneratorsTargetDomainEventsDeletedEventMapper = new();
    private static readonly Dictionary<string, Func<string, string, IEvent>> _deserializers = new();
    private readonly IEventStore _eventStore;

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
        List<IEventData>? events;
        try
        {
            var eventData = await _eventStore.GetAsync(id, cancellationToken);
            events = eventData.ToList();
        } 
        catch (Exception ex)
        {
            return new Error($"Error while getting events for aggregate with id '{id}'. #GetEventsFailed").CausedBy(ex);
        }
        
        var eventInstances = new List<IEvent>();
        foreach (var evt in events)
        {
            try
            {
                var eventInstance = Deserialize(evt.Schema, evt.Data);
                eventInstances.Add(eventInstance);
            } 
            catch (Exception ex)
            {
                return new Error($"Error while deserializing event with schema '{evt.Schema}' from stream with id '{id}'. #DeserializeEventFailed").CausedBy(ex);
            }
        }

        try
        {
            MyTestAggregate? aggregate = null;
            foreach (var evt in eventInstances)
                aggregate = aggregate == null ? CreateFromEvent(evt) : ApplyEvent(aggregate, evt);

            if (aggregate is null)
                return new Error($"No events found for aggregate with id '{id}'. #NoEventsFound");

            return new Aggregate<MyTestAggregate>(aggregate, events.Max(x => x.Version));
        }
        catch (Exception ex)
        {
            return new Error($"Error while applying events to aggregate with id '{id}'. #ApplyEventsFailed").CausedBy(ex);
        }
    }

    public async Task<Result> SaveAsync(Guid id, IEnumerable<IEvent> events, int expectedVersion, CancellationToken cancellationToken)
    {
        // TODO: Hier fehlt noch Logik und Versioning!
        var serializedEvents = new List<EventData>();
        try
        {
            foreach (var evt in events)
            {
                var serializedEvent = Serialize(evt);
                var eventData = new EventData
                {
                    Id = Guid.NewGuid(),
                    Created = DateTimeOffset.Now,
                    StreamId = id,
                    //TODO: Version is WRONG atm.
                    Version = expectedVersion + serializedEvents.Count + 1, // Increment version for each event
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
    }

    public MyTestAggregate SaveAndUpdate(Guid id, IEnumerable<IEvent> events)
    {
        Save(id, events);
        return Get(id);
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