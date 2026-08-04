namespace EventSourcing.Mappers;

public interface IEventMapper
{
    IEnumerable<string> Schemas { get; }
    Type EventType { get; }
}

public interface IEventMapper<TEvent> : IEventMapper where TEvent: IEvent
{
    ISerializedEvent Serialize(TEvent @event);
    
    TEvent Deserialize(string schema, string data);
}