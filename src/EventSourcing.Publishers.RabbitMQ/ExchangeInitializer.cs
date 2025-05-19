using RabbitMQ.Client;

namespace EventSourcing.Publishers.RabbitMQ;

public class ExchangeInitializer
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly string _baseExchangeName;

    public ExchangeInitializer(IConnectionFactory connectionFactory, string baseExchangeName)
    {
        _connectionFactory = connectionFactory;
        _baseExchangeName = baseExchangeName;
    }

    public async Task Initialize()
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        
        await channel.ExchangeDeclareAsync(_baseExchangeName, ExchangeType.Topic, true, false);
    }
}