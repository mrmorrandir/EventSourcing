using System.Diagnostics.CodeAnalysis;
using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EventSourcing.Mappers;
using EventSourcing.Projections;
using EventSourcing.Publishers.RabbitMQ.IntegrationTests.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Testcontainers.RabbitMq;

namespace EventSourcing.Publishers.RabbitMQ.IntegrationTests;

[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
public class RabbitMqEventPublisherTests : IAsyncLifetime
{
    private IContainer? _rabbitMqContainer;

    /// <summary>
    /// Sets up the RabbitMQ container for testing.
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:4.1-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .WithHostname("localhost")
            .WithPortBinding(5672, 5672) // RabbitMQ default port
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5672))
            .Build();
        await _rabbitMqContainer.StartAsync();
    }

    /// <summary>
    /// Cleans up the RabbitMQ container after tests are completed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_rabbitMqContainer != null)
            await _rabbitMqContainer.DisposeAsync();
    }
    
    [Fact]
    public async Task PublishAsync_ShouldSucceed_WhenDataIsValid()
    {
        // Arrange Services
        const string exchangeName = "testExchange";
        const string queueName = "testQueue";
        var serviceProvider = GetServices(exchangeName);
        await serviceProvider.UseRabbitMqPublishing();
        
        // Arrange Variables
        var testAggregateSerializationRegistry = serviceProvider.GetRequiredService<ISerializationRegistry<TestAggregate>>();
        var rabbitMqConnectionFactory = serviceProvider.GetRequiredService<IConnectionFactory>();
        var rabbitMqEventProjectors = serviceProvider.GetRequiredService<IEnumerable<IProjector<TestAggregate>>>();
        var rabbitMqEventPublisher = rabbitMqEventProjectors
            .Where(p => p is RabbitMqEventPublisher<TestAggregate>)
            .Cast<RabbitMqEventPublisher<TestAggregate>>()
            .First();
        
        // Arrange RabbitMQ Test Connection
        await using var rabbitMqConnection = await rabbitMqConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var rabbitMqChannel = await rabbitMqConnection.CreateChannelAsync(cancellationToken:TestContext.Current.CancellationToken);
        await rabbitMqChannel.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic, true, false, null, cancellationToken:TestContext.Current.CancellationToken);
        await rabbitMqChannel.QueueDeclareAsync(queueName, false, false, true, null, cancellationToken:TestContext.Current.CancellationToken);
        await rabbitMqChannel.QueueBindAsync(queueName, exchangeName, "#", null, cancellationToken:TestContext.Current.CancellationToken);
        var rabbitMqConsumer = new AsyncEventingBasicConsumer(rabbitMqChannel);
        var receivedEventDeserializationResults = new List<Result<IEvent>>();
        rabbitMqConsumer.ReceivedAsync += (sender, eventArgs) =>
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            var type = eventArgs.RoutingKey.Replace(".", "-");
            var deserializeResult = testAggregateSerializationRegistry.Deserialize(type, json);
            receivedEventDeserializationResults.Add(deserializeResult);            
            return Task.CompletedTask;
        };
        await rabbitMqChannel.BasicConsumeAsync(queueName, true, rabbitMqConsumer, cancellationToken:TestContext.Current.CancellationToken);
        
        // Arrange Aggregate and Event
        var testAggregateCreatedEvent = new CreatedEvent(Guid.NewGuid(), "Test Name", "Test Description");
        var testAggregate = TestAggregate.Create(testAggregateCreatedEvent);

        // Act
        var projectResult = await rabbitMqEventPublisher.ProjectAsync(testAggregate, testAggregateCreatedEvent, cancellationToken:TestContext.Current.CancellationToken);
        
        // Assert
        await Task.Delay(100, TestContext.Current.CancellationToken);
        projectResult.IsSuccess.Should().BeTrue($"because the event should be published successfully (without errors like {projectResult.Errors.FirstOrDefault()?.Message})");
        receivedEventDeserializationResults.Should().NotBeEmpty("because we should receive the event in RabbitMQ");
        receivedEventDeserializationResults.Count.Should().Be(1, "because we published only one event");
        var receivedEvent = receivedEventDeserializationResults.First();
        receivedEvent.IsSuccess.Should().BeTrue("because the event should be deserialized successfully");
        receivedEvent.Value.Should().BeOfType<CreatedEvent>("because we published a CreatedEvent");
        var createdEvent = (CreatedEvent)receivedEvent.Value;
        createdEvent.AggregateId.Should().Be(testAggregateCreatedEvent.AggregateId, "because the aggregate ID should match the published event");
        createdEvent.Name.Should().Be(testAggregateCreatedEvent.Name, "because the name should match the published event");
        createdEvent.Description.Should().Be(testAggregateCreatedEvent.Description, "because the description should match the published event");
    }
    
    [Fact]
    public async Task ExchangeInitializers_ShouldRegisterExchangeOnStartup()
    {
        // Arrange Services
        const string exchangeName = "testExchange";
        var serviceProvider = GetServices(exchangeName);
        await serviceProvider.UseRabbitMqPublishing();
        
        // Arrange RabbitMQ Test Connection
        var rabbitMqConnectionFactory = serviceProvider.GetRequiredService<IConnectionFactory>();
        await using var rabbitMqConnection = await rabbitMqConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var rabbitMqChannel = await rabbitMqConnection.CreateChannelAsync(cancellationToken:TestContext.Current.CancellationToken);
        
        // Act
        var func = async () => await rabbitMqChannel.ExchangeDeclarePassiveAsync(exchangeName, cancellationToken:TestContext.Current.CancellationToken);

        // Assert
        await func.Should().NotThrowAsync("because the exchange should be registered on startup when UseRabbitMqPublishing is called");
    }
    
    [Fact]
    public async Task ExchangeInitializers_ShouldNotRegisterExchangeOnStartUp_WhenUseRabbitMQPublishingIsNotCalled()
    {
        // Arrange Services
        const string exchangeName = "testExchange";
        var serviceProvider = GetServices(exchangeName);
        // Note: Do not call UseRabbitMqPublishing here to simulate the scenario where the exchange is not registered.
        
        // Arrange RabbitMQ Test Connection
        var connectionFactory = serviceProvider.GetRequiredService<IConnectionFactory>();
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken:TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken:TestContext.Current.CancellationToken);

        // Act
        var func = async () => await channel.ExchangeDeclarePassiveAsync(exchangeName, cancellationToken:TestContext.Current.CancellationToken);

        // Assert
        await func.Should().ThrowAsync<OperationInterruptedException>("because the exchange should not exist if UseRabbitMqPublishing is not called");
    }

    private ServiceProvider GetServices(string exchangeName)
    {
        var services = new ServiceCollection();
        services.AddRabbitMqPublishing(options => options
            .UseConnection("localhost", "guest", "guest")
            .UseBaseExchangeName(exchangeName));
        services.AddRabbitMqEventPublisher<TestAggregate>();
        services.AddEventSourcing();
        return services.BuildServiceProvider();        
    }
}