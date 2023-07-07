namespace EventSourcing.Abstractions.Mappers;

public interface IEventRegistry 
{
    ISerializedEvent Serialize(IEvent @event);
    IEvent Deserialize(string type, string data);
}