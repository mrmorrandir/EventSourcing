namespace EventSourcing.Abstractions.Mappers;

public interface IEventMap<TEvent> where TEvent : IEvent
{
    TEvent Map(string data);
    //bool TryMap(string data, out TEvent @event);
    
    string Map(TEvent @event);
    //bool TryMap(TEvent @event, out string data);
}