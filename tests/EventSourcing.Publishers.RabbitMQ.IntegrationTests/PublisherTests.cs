using System.Reflection;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EventSourcing.Mappers;
using EventSourcing.Publishers.RabbitMQPublisher;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EventSourcing.Publishers.RabbitMQ.IntegrationTests;

public class PublisherTests : IAsyncLifetime
{
    private IContainer? _rabbitMqContainer;

    [Fact]
    public async Task PublishAsync_ShouldSucceed_WhenDataIsValid()
    {
        const string exchangeName = "testExchange";
        const string queueName = "testQueue";
        var received = false;
        RabbitTestEvent? receivedEvent = null;
        var rabbitMqTestEvent = new RabbitTestEvent(Guid.NewGuid(), "Test");
        var serviceProvider = GetServices(exchangeName);
        serviceProvider.UseRabbitMQPublishing();
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
        
        await Task.Delay(100);
        received.Should().BeTrue();
        receivedEvent.Should().BeEquivalentTo(rabbitMqTestEvent);
    }
    
    [Fact]
    public async Task ExchangeInitializers_ShouldRegisterExchangeOnStartup()
    {
        const string exchangeName = "testExchange";
        var serviceProvider = GetServices(exchangeName);
        serviceProvider.UseRabbitMQPublishing();
        var connectionFactory = serviceProvider.GetRequiredService<IAsyncConnectionFactory>();
        using var connection = connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();
        
        var func = () => channel.ExchangeDeclarePassive(exchangeName);
        
        func.Should().NotThrow();
    }
    
    [Fact]
    public async Task ExchangeInitializers_ShouldNotRegisterExchangeOnStartUp_WhenUseRabbitMQPublishingIsNotCalled()
    {
        const string exchangeName = "testExchange";
        var serviceProvider = GetServices(exchangeName);
        var connectionFactory = serviceProvider.GetRequiredService<IAsyncConnectionFactory>();
        using var connection = connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();
        
        var func = () => channel.ExchangeDeclarePassive(exchangeName);
        
        func.Should().Throw<OperationInterruptedException>();
    }

    private ServiceProvider GetServices(string exchangeName)
    {
        var services = new ServiceCollection();
        services.AddEventSourcing(config =>
        {
            config.ConfigureMapping(options => options.AddMappers(Assembly.GetExecutingAssembly()).IgnoreUncoveredEvents());
            config.ConfigureProjections(options => options.IgnoreUncoveredEvents());
            config.ConfigureEventStoreDbContext(options => options.UseInMemoryDatabase("TestDatabase"));
            config.AddRabbitMQPublishing(options =>
            {
                options.UseConnection("localhost", "guest", "guest");
                options.UseBaseExchangeName(exchangeName);
                options.AddPublisher<RabbitTestEvent>();
            });
        });
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider;
    }

    public async Task InitializeAsync()
    {
        _rabbitMqContainer = new ContainerBuilder()
            .WithImage("rabbitmq:3.13-management")
            .WithPortBinding(5672, 5672)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5672))
            .Build();
        await _rabbitMqContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_rabbitMqContainer != null)
            await _rabbitMqContainer.DisposeAsync();
    }
}

public record RabbitTestEvent(Guid Id, string Text) : IEvent;