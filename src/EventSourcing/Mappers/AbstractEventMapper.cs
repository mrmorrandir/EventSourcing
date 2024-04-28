using System.Text.Json;
using System.Text.RegularExpressions;

namespace EventSourcing.Mappers;

public abstract class AbstractEventMapper<TEvent> : EventMapper, IEventMapper<TEvent> where TEvent : IEvent
{
    private readonly List<string> _types = new();
    private readonly Dictionary<string, IEventDeserializer<TEvent>> _deserializers = new();
    private IEventSerializer<TEvent> _serializer = null!;

    public IEnumerable<string> Types => _types;
    public Type EventType => typeof(TEvent);

    public ISerializedEvent Serialize(TEvent @event)
    {
        if (_serializer == null)
            throw new InvalidOperationException($"Serializer for type {@event.GetType().Name} not registered");
        
        return _serializer.Serialize(@event);
    }

    public TEvent Deserialize(string type, string data)
    {
        if (!_deserializers.ContainsKey(type))
            throw new InvalidOperationException($"Deserializer for type {type} not registered");
        
        return _deserializers[type].Deserialize(data);
    }

    /// <summary>
    /// Configures a serializer for the specified type. Only one serializer can be registered per instance.
    /// <para>
    /// The type name should be in kebab case with a version number in the end. For example: <c>my-event-v1</c>.
    /// This is to ensure that the type name is unique and can be used to identify the event type. It also makes it easier to support versioning of events.
    /// </para>
    /// </summary>
    /// <param name="serializer">The serializer to be used</param>
    /// <exception cref="InvalidOperationException">When serializer is already registered</exception>
    protected void WillSerialize(IEventSerializer<TEvent> serializer)
    {
        if (_serializer != null)
            throw new InvalidOperationException($"Serializer for type {serializer.Type} already registered");
        if (!TypeRegex.IsMatch(serializer.Type))
            throw new ArgumentException($"Serializer type {serializer.Type} must be in kebab case with a version number in the end", nameof(serializer));
        
        _serializer = serializer;
    }
    
    /// <summary>
    /// Configures a default json serializer for the specified type. Only one serializer can be registered per instance.
    /// </summary>
    /// <param name="type">The type name to be used</param>
    protected void WillSerialize(string type)
    {
        if (!TypeRegex.IsMatch(type))
            throw new ArgumentException($"Serializer type {type} must be in kebab case with a version number in the end", nameof(type));
        WillSerialize(new EventSerializer<TEvent>(type));
    }

    /// <summary>
    /// Configures a deserializer for a specific type. Multiple deserializers can be registered per instance.
    /// <para>
    /// The multiple deserializers are used to support versioning of events. When a new version of an event is created,
    /// a new deserializer can be registered for the new type name. The old deserializer can be kept for backwards compatibility.
    /// </para>
    /// </summary>
    /// <param name="type">The type name to be used</param>
    /// <param name="deserializer">The deserializer to be used</param>
    protected void CanDeserialize(string type, IEventDeserializer<TEvent> deserializer)
    {
        _deserializers.Add(type, deserializer);
        _types.Add(type);
    }
    
    /// <summary>
    /// Configures a deserializer for a specific type. Multiple deserializers can be registered per instance.
    /// <para>
    /// The multiple deserializers are used to support versioning of events. When a new version of an event is created,
    /// a new deserializer can be registered for the new type name. The old deserializer can be kept for backwards compatibility.
    /// </para>
    /// </summary>
    /// <param name="type">The type name to be used</param>
    /// <param name="deserializer">The deserializer function to be used</param>
    protected void CanDeserialize(string type, Func<string, JsonSerializerOptions, TEvent> deserializer)
    {
        CanDeserialize(type, new EventDeserializer<TEvent>(deserializer));
    }

    /// Configures a default json deserializer for a specific type. Multiple deserializers can be registered per instance.
    /// <para>
    /// The multiple deserializers are used to support versioning of events. When a new version of an event is created,
    /// a new deserializer can be registered for the new type name. The old deserializer can be kept for backwards compatibility.
    /// </para>
    /// </summary>
    /// <param name="type">The type name to be used</param>
    protected void CanDeserialize(string type)
    {
        CanDeserialize(type, new EventDeserializer<TEvent>());
    }
}