namespace EventSourcing.Mappers;

public interface IEventMapper
{
    IEnumerable<string> Types { get; }
    Type EventType { get; }
}

public interface IEventMapper<TEvent> : IEventMapper where TEvent: IEvent
{
    ISerializedEvent Serialize(TEvent @event);
    
    TEvent Deserialize(string type, string data);
}