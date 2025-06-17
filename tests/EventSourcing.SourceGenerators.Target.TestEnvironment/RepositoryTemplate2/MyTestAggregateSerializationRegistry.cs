using System.Collections.Concurrent;
using System.Reflection;
using EventSourcing.Mappers;
using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;
using FluentResults;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

/// <summary>
/// This better be source-generated
/// </summary>
public class MyTestAggregateSerializationRegistry : ISerializationRegistry<MyTestAggregate>
{
    private static readonly StateSerializer<MyTestAggregate> _stateSerializer = new();
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

    public Result<ISerializedState> Serialize(MyTestAggregate state) => Result.Try(() => _stateSerializer.Serialize(state));

    public Result<ISerializedEvent> Serialize(IEvent @event)
    {
        return @event switch
        {
            CreatedEvent createdEvent when @event.GetType() == typeof(CreatedEvent) => Serialize(createdEvent),
            ChangedNameEvent changedNameEvent when @event.GetType() == typeof(ChangedNameEvent) => Serialize(changedNameEvent),
            ChangedDescriptionEvent changedDescriptionEvent when @event.GetType() == typeof(ChangedDescriptionEvent) => Serialize(changedDescriptionEvent),
            DeletedEvent deletedEvent when @event.GetType() == typeof(DeletedEvent) => Serialize(deletedEvent),
            _ => new Error($"No serializer found for event type {@event.GetType().Name}")
        };
    }
    
    public Result<ISerializedEvent> Serialize(CreatedEvent @event) => Result.Try(() => _eventSourcingSourceGeneratorsTargetDomainEventsCreatedEventMapper.Serialize(@event));
    
    public Result<ISerializedEvent> Serialize(ChangedNameEvent @event) => Result.Try(() => _eventSourcingSourceGeneratorsTargetDomainEventsChangedNameEventMapper.Serialize(@event));
    
    public Result<ISerializedEvent> Serialize(ChangedDescriptionEvent @event) => Result.Try(() => _eventSourcingSourceGeneratorsTargetDomainEventsChangedDescriptionEventMapper.Serialize(@event));
    
    public Result<ISerializedEvent> Serialize(DeletedEvent @event) => Result.Try(() => _eventSourcingSourceGeneratorsTargetDomainEventsDeletedEventMapper.Serialize(@event));

    public Result<IEvent> Deserialize(string schema, string data)
    {
        if (!_deserializers.TryGetValue(schema, out var deserializer))
            return new Error($"No deserializer found for type {schema}");

        return Result.Try(() => deserializer(schema, data));
    }
}