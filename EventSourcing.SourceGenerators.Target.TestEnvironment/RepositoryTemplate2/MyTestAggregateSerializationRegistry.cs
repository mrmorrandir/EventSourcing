using EventSourcing.Mappers;
using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.Events;
using FluentResults;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

/// <summary>
/// This better be source-generated
/// </summary>
public class MyTestAggregateSerializationRegistry : ISerializationRegistry<MyTestAggregate>
{
    private static readonly CreatedEventMapper _eventSourcingSourceGeneratorsTargetDomainEventsCreatedEventMapper = new();
    private static readonly ChangedNameEventMapper _eventSourcingSourceGeneratorsTargetDomainEventsChangedNameEventMapper = new();
    private static readonly ChangedDescriptionEventMapper _eventSourcingSourceGeneratorsTargetDomainEventsChangedDescriptionEventMapper = new();
    private static readonly DeletedEventMapper _eventSourcingSourceGeneratorsTargetDomainEventsDeletedEventMapper = new();
    private static readonly Dictionary<string, Func<string, string, IEvent>> _deserializers = new();
    

    static MyTestAggregateSerializationRegistry()
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
    
    public Result<ISerializedEvent> Serialize(IEvent @event)
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

    public Result<IEvent> Deserialize(string schema, string data)
    {
        if (!_deserializers.TryGetValue(schema, out var deserializer))
            throw new EventRegistryException($"No deserializer found for type {schema}");

        return Result.Try(() => deserializer(schema, data));
    }
}