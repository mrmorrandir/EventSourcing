namespace EventSourcing.Publishers.RabbitMQPublisher;

public class PublisherEvent
{
    public Type EventType { get; }
    public string? BaseExchangeName { get; }
    
    public PublisherEvent(Type eventType, string? baseExchangeName)
    {
        EventType = eventType;
        BaseExchangeName = baseExchangeName;
    }
}