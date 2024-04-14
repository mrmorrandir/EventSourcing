using EventSourcing.Repositories;

namespace EventSourcing.Mappers;

public class EventRegistry : IEventRegistry
{
    private readonly IEnumerable<IEventMapper> _eventMappers;
    private readonly ILookup<string, Func<string, string, IEvent>> _deserializerLookup;
    private readonly ILookup<Type, Func<IEvent, ISerializedEvent>> _serializerLookup;

    public EventRegistry(IEnumerable<IEventMapper> eventMappers)
    {
        _eventMappers = eventMappers;
        // TODO: Check for doublicate "Types" in eventMappers (the string as well as the event type)
        _serializerLookup = _eventMappers
            .ToLookup(
                eventMapper => eventMapper.EventType, 
                eventMapper =>
                {
                    var serializeMethod = eventMapper.GetType().GetMethod("Serialize")!;
                    var serializeDelegate = (Func<IEvent, ISerializedEvent>)(@event => (ISerializedEvent)serializeMethod.Invoke(eventMapper, new object[] { @event }));
                    return serializeDelegate;
                });
        _deserializerLookup = _eventMappers
            .SelectMany(em => em.Types.Select(t => new { Type = t, Mapper = em }))
            .ToLookup(
                typeAndMapper => typeAndMapper.Type, 
                typeAndMapper =>
                {
                    var deserializeMethod = typeAndMapper.Mapper.GetType().GetMethod("Deserialize")!;
                    var deserializeDelegate = (Func<string, string, IEvent>)((type, data) => (IEvent)deserializeMethod.Invoke(typeAndMapper.Mapper, new object[] { type, data }));
                    return deserializeDelegate;
                });
    }

    public ISerializedEvent Serialize(IEvent @event)
    {
        var serializer = _serializerLookup[@event.GetType()].FirstOrDefault();
        if (serializer is null)
            throw new EventRegistryException($"Event mapper for type {@event.GetType().Name} not found.");
        
        return serializer(@event);
    }

    public IEvent Deserialize(string type, string data)
    {
        var deserializer = _deserializerLookup[type].FirstOrDefault();
        if (deserializer is null)
            throw new EventRegistryException($"Event mapper for type {type} not found.");
        
        return deserializer(type, data);
    }
}