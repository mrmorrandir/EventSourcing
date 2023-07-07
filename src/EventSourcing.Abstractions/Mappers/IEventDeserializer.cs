namespace EventSourcing.Abstractions.Mappers;

public interface IEventDeserializer<TEvent> where TEvent : IEvent
{
    TEvent Deserialize(string data);
}