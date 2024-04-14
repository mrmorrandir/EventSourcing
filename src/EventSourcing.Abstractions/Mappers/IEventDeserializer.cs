namespace EventSourcing.Mappers;

public interface IEventDeserializer<TEvent> where TEvent : IEvent
{
    TEvent Deserialize(string data);
}