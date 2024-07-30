using System.Text;
using EventSourcing.Mappers;
using RabbitMQ.Client;

namespace EventSourcing.Publishers.RabbitMQPublisher;

public class Publisher<TEvent> : IPublisher<TEvent> where TEvent : IEvent
{
    private readonly IAsyncConnectionFactory _asyncConnectionFactory;
    private readonly IEventRegistry _eventRegistry;
    private readonly string _baseExchangeName;

    public Publisher(IAsyncConnectionFactory asyncConnectionFactory, IEventRegistry eventRegistry, string baseExchangeName)
    {
        _asyncConnectionFactory = asyncConnectionFactory;
        _eventRegistry = eventRegistry;
        _baseExchangeName = baseExchangeName;
    }
    
    public Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default)
    {
        return PublishAsync(@event, cancellationToken);
    }
    
    public Task PublishAsync(TEvent @event, CancellationToken cancellationToken = default)
    {
        using var connection = _asyncConnectionFactory.CreateConnection();
        using var channel = connection.CreateModel();
        
        channel.ExchangeDeclare(_baseExchangeName, ExchangeType.Topic, true, false, null);
        var serializedEvent = _eventRegistry.Serialize(@event);

        var routingKey = serializedEvent.Type.Replace("-", ".");
        var body = Encoding.UTF8.GetBytes(serializedEvent.Data);
        channel.BasicPublish(
            _baseExchangeName, 
            routingKey, 
            basicProperties: null, 
            body: body);
        
        return Task.CompletedTask;
    }
}