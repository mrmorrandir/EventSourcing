using System.Text;
using System.Text.Json;
using EventSourcing.Mappers;
using EventSourcing.Publishers.RabbitMQ.DI;
using FluentResults;
using RabbitMQ.Client;

namespace EventSourcing.Publishers.RabbitMQ;

public class RabbitMqStatePublisher<TAggregate> : IPublisher<TAggregate> where TAggregate : IAggregate
{
    private readonly string _routingKey; 
    private readonly IConnectionFactory _connectionFactory;
    private readonly RabbitMqPublisherOptions _options;
    private readonly bool _failOnError;
    
    public RabbitMqStatePublisher(RabbitMqPublisherOptions options, IConnectionFactory connectionFactory, bool failOnError = true)
    {
        _connectionFactory = connectionFactory;
        _options = options;
        _failOnError = failOnError;
        _routingKey = $"{typeof(TAggregate).Name.ToLowerInvariant()}.state";
    }

    public async Task<Result> ProjectAsync(TAggregate state, IEvent @event, CancellationToken cancellationToken = default)
    {
        var publishResult = await PublishAsync(state, cancellationToken);
        if (publishResult.IsFailed && _failOnError)
            return Result.Fail(publishResult.Errors);

        return Result.Ok();
    }

    private async Task<Result> PublishAsync(TAggregate state, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        
        await channel.ExchangeDeclareAsync(_options.BaseExchangeName, ExchangeType.Topic, true, false, cancellationToken: cancellationToken);
        var serializationResult = Result.Try(() => JsonSerializer.Serialize(state, EventSerializerOptions.Default));
        if (serializationResult.IsFailed)
            return Result.Fail(serializationResult.Errors);

        var serializedState = serializationResult.Value;
        var body = Encoding.UTF8.GetBytes(serializedState);
        return await Result.Try(() => channel.BasicPublishAsync(_options.BaseExchangeName, _routingKey, body, cancellationToken));
    }
}