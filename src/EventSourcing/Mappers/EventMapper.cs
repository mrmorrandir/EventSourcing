using EventSourcing.Abstractions;
using EventSourcing.Abstractions.Mappers;

namespace EventSourcing.Mappers;
public class EventMapper : IEventMapper
{
    private readonly Dictionary<string, Type> _eventTypes = new Dictionary<string, Type>();
    private readonly Dictionary<Type, Func<string, IEvent>> _deserializationMaps = new();
    private readonly Dictionary<Type, Func<IEvent, string>> _serializationMaps = new();

    public void Register<TEvent>(string type, IEventMap<TEvent> eventMap) where TEvent : IEvent
    {
        if (type is null) throw new ArgumentNullException(nameof(type));
        if (eventMap is null) throw new ArgumentNullException(nameof(eventMap));
        if (_serializationMaps.ContainsKey(typeof(TEvent))) throw new ArgumentException($"Map for type {type} already registered");
        if (_deserializationMaps.ContainsKey(typeof(TEvent))) throw new ArgumentException($"Map for type {typeof(TEvent)} already registered");
        
        _eventTypes.Add(type, typeof(TEvent));
        _serializationMaps.Add(typeof(TEvent), @event => eventMap.Map((TEvent)@event));
        _deserializationMaps.Add(typeof(TEvent), s => eventMap.Map(s));
    }

    public void Register<TEvent>(string type) where TEvent : IEvent
    {
        // create a default EventMap<>
        var eventMap = new EventMap<TEvent>();
        Register(type, eventMap);
    }

    public string GetTypeName(IEvent @event)
    {
        if (@event is null) throw new ArgumentNullException(nameof(@event));
        var type = @event.GetType();
        if (!_eventTypes.ContainsValue(type)) throw new ArgumentException($"No map for type {type} registered");
            
        return _eventTypes.First(x => x.Value == type).Key;
    }

    public IEvent Map(string type, string data)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (type is null) throw new ArgumentNullException(nameof(type));
        if (!_eventTypes.ContainsKey(type)) throw new ArgumentException($"Map for type {type} not registered");
        if (!_deserializationMaps.ContainsKey(_eventTypes[type])) throw new ArgumentException($"Map for type {_eventTypes[type]} not registered");
        
        return _deserializationMaps[_eventTypes[type]](data);
    }
   
    public string Map(IEvent @event)
    {
        if (@event is null) throw new ArgumentNullException(nameof(@event));
        var type = @event.GetType();
        if (!_serializationMaps.ContainsKey(type)) throw new ArgumentException($"Map for type {type} not registered");
        
        return _serializationMaps[type](@event);
    }
}