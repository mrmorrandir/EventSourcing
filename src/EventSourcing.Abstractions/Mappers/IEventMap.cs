namespace EventSourcing.Abstractions.Mappers;

public interface IEventMap<TEvent> where TEvent : IEvent
{
    TEvent Map(string type, string data);
    
    string Map(TEvent @event);
}