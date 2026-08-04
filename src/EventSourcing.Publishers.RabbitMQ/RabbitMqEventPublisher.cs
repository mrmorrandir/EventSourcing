using System.Globalization;
using System.Text;
using EventSourcing.Mappers;
using EventSourcing.Projections;
using EventSourcing.Publishers.RabbitMQ.DI;
using FluentResults;
using RabbitMQ.Client;

namespace EventSourcing.Publishers.RabbitMQ;

public class RabbitMqEventPublisher<TAggregate> : IPublisher<TAggregate> where TAggregate : IAggregate
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly RabbitMqPublisherOptions _options;
    private readonly ISerializationRegistry<TAggregate> _serializationRegistry;
    private readonly bool _failOnError;

    public RabbitMqEventPublisher(RabbitMqPublisherOptions options, IConnectionFactory connectionFactory, ISerializationRegistry<TAggregate> serializationRegistry, bool failOnError = true)
    {
        _connectionFactory = connectionFactory;
        _options = options;
        _serializationRegistry = serializationRegistry;
        _failOnError = failOnError;
    }

    public async Task<Result> ProjectAsync(TAggregate state, IEvent @event, CancellationToken cancellationToken = default)
    {
        var publishResult = await PublishAsync(@event, cancellationToken);
        if (publishResult.IsFailed && _failOnError)
            return Result.Fail(publishResult.Errors);

        return Result.Ok();
    }

    private async Task<Result> PublishAsync(IEvent @event, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        
        await channel.ExchangeDeclareAsync(_options.BaseExchangeName, ExchangeType.Topic, true, false, cancellationToken: cancellationToken);
        var serializationResult = _serializationRegistry.Serialize(@event);
        if (serializationResult.IsFailed)
            return Result.Fail(serializationResult.Errors);

        var serializedEvent = serializationResult.Value;
        var routingKey = serializedEvent.Schema.Replace("-", ".");
        var body = Encoding.UTF8.GetBytes(serializedEvent.Data);
        return await Result.Try(() => channel.BasicPublishAsync(_options.BaseExchangeName, routingKey, body, cancellationToken));
    }
}