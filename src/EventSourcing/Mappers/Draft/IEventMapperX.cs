using EventSourcing.Abstractions;

namespace EventSourcing.Mappers;

public interface IEventMapperX
{
    IEnumerable<string> Types { get; }
    Type EventType { get; }
}

public interface IEventMapperX<TEvent> : IEventMapperX where TEvent: IEvent
{
    ISerializedEvent Serialize(TEvent @event);
    
    TEvent Deserialize(string type, string data);
}