namespace EventSourcing.Abstractions.Mappers;

public interface IEventRegistration
{
    void Register<TEvent>(string type, IEventMap<TEvent> eventMap) where TEvent : IEvent;
    void Register<TEvent>(string type) where TEvent : IEvent;
}

public interface IEventMapper : IEventRegistration
{
    string GetTypeName(IEvent @event);
    
    IEvent Map(string type, string data);
    
    string Map(IEvent @event);
}