namespace EventSourcing.Abstractions;

public interface IEventHandler<in TEvent> : IEventHandler where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

public interface IEventHandler
{
}