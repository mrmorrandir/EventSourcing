using RabbitMQ.Client;

namespace EventSourcing.Publishers.RabbitMQPublisher;

public class ExchangeInitializer
{
    private readonly IAsyncConnectionFactory _asyncConnectionFactory;
    private readonly string _baseExchangeName;

    public ExchangeInitializer(IAsyncConnectionFactory asyncConnectionFactory, string baseExchangeName)
    {
        _asyncConnectionFactory = asyncConnectionFactory;
        _baseExchangeName = baseExchangeName;
    }

    public void Initialize()
    {
        using var connection = _asyncConnectionFactory.CreateConnection();
        using var channel = connection.CreateModel();
        
        channel.ExchangeDeclare(_baseExchangeName, ExchangeType.Topic, true, false, null);
    }
}