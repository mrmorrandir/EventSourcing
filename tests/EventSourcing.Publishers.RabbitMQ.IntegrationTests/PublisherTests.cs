using System.Reflection;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Text.Json;
using EventSourcing.Mappers;
using EventSourcing.Publishers.RabbitMQPublisher;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventSourcing.Publishers.RabbitMQ.IntegrationTests;

public class PublisherTests
{
    [Fact]
    public async Task ShouldWork()
    {
        const string exchangeName = "testExchange";
        const string queueName = "testQueue";
        var received = false;
        RabbitTestEvent? receivedEvent = null;
        var rabbitMqTestEvent = new RabbitTestEvent(Guid.NewGuid(), "Test");
        var services = new ServiceCollection();
        services.AddRabbitMQPublishers(config =>
        {
            config.AddDefaultPublishersForAssembly(Assembly.GetExecutingAssembly());
            config.BaseExchangeName = exchangeName;
        });
        services.AddEventSourcing(config =>
        {
            config.ConfigureEventStoreDbContext(options => options.UseInMemoryDatabase("TestDatabase"));
        });
        var serviceProvider = services.BuildServiceProvider();
        var eventRegistry = serviceProvider.GetRequiredService<IEventRegistry>();
        var connectionFactory = serviceProvider.GetRequiredService<IAsyncConnectionFactory>();
        using var connection = connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(exchangeName, ExchangeType.Topic, true, false, null);
        channel.QueueDeclare(queueName, false, false, true, null);
        channel.QueueBind(queueName, exchangeName, "#", null);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += (sender, eventArgs) =>
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            var type = eventArgs.RoutingKey.Replace(".", "-");
            var @event = (RabbitTestEvent)eventRegistry.Deserialize(type, json);
            received = true;
            receivedEvent = @event;
            return Task.CompletedTask;
        };
        channel.BasicConsume(queueName, true, consumer);
        var publisher = serviceProvider.GetRequiredService<IPublisher<RabbitTestEvent>>();

        await publisher.PublishAsync(rabbitMqTestEvent);
        
        //await Task.Delay(10);
        received.Should().BeTrue();
        receivedEvent.Should().BeEquivalentTo(rabbitMqTestEvent);
    }
}

public record RabbitTestEvent(Guid Id, string Text) : IEvent;