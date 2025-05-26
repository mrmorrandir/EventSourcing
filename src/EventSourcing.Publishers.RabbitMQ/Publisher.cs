using System.Text;
using EventSourcing.Mappers;
using RabbitMQ.Client;

namespace EventSourcing.Publishers.RabbitMQ;

public class Publisher<TEvent> : IPublisher<TEvent> where TEvent : IEvent
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly IEventRegistry _eventRegistry;
    private readonly string _baseExchangeName;

    public Publisher(IConnectionFactory connectionFactory, IEventRegistry eventRegistry, string baseExchangeName)
    {
        _connectionFactory = connectionFactory;
        _eventRegistry = eventRegistry;
        _baseExchangeName = baseExchangeName;
    }
    
    public Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default)
    {
        return PublishAsync(@event, cancellationToken);
    }
    
    public async Task PublishAsync(TEvent @event, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        
        await channel.ExchangeDeclareAsync(_baseExchangeName, ExchangeType.Topic, true, false, cancellationToken: cancellationToken);
        var serializedEvent = _eventRegistry.Serialize(@event);

        var routingKey = serializedEvent.Schema.Replace("-", ".");
        var body = Encoding.UTF8.GetBytes(serializedEvent.Data);
        await channel.BasicPublishAsync(_baseExchangeName, routingKey, body, cancellationToken);
    }
}