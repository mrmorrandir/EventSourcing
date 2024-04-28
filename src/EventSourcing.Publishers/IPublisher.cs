namespace EventSourcing.Publishers;

public interface IPublisher<TEvent> : IEventHandler<TEvent> where TEvent : IEvent
{
    Task PublishAsync(TEvent @event, CancellationToken cancellationToken = default);
}