namespace EventSourcing.Projections;

public interface IProjection<in TEvent> : IEventHandler<TEvent> where TEvent : IEvent
{
    
}