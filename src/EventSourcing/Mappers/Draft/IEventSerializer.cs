using EventSourcing.Abstractions;

namespace EventSourcing.Mappers;

public interface IEventSerializer<TEvent> where TEvent : IEvent
{
    ISerializedEvent Serialize(TEvent @event);
    string Type { get; }
}