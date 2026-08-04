
using EventSourcing;
using EventSourcing.Projections;
using EventSourcing.Publishers;
using EventSourcing.Publishers.RabbitMQ;
using EventSourcing.Publishers.RabbitMQ.DI;
using RabbitMQ.Client;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class RabbitMqPublishingDependencyInjection
{
    /// <summary>
    /// <para>
    /// Adds RabbitMQ publishing services to the service collection.
    /// </para>
    /// <para>
    /// Registered services include:
    /// <list type="bullet">
    /// <item><see cref="RabbitMqPublisherOptions"/></item>
    /// <item><see cref="IConnectionFactory"/></item>
    /// <item><see cref="ExchangeInitializer"/></item>
    /// </list>
    /// </para>
    /// <para>
    /// To use publishers, you must also register them using <see cref="AddRabbitMqEventPublisher{TPublisher,TAggregate}"/> or their own specific registration methods.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The <see cref="UseRabbitMqPublishing(IServiceProvider)"/> method must be called after the service provider is built to initialize RabbitMQ exchanges.
    /// </remarks>
    public static IServiceCollection AddRabbitMqPublishing(this IServiceCollection services, Action<RabbitMqPublisherOptionsBuilder> options)
    {
        var builder = new RabbitMqPublisherOptionsBuilder();
        options(builder);
        var publisherOptions = builder.Build();
        
        services.AddSingleton<RabbitMqPublisherOptions>(publisherOptions);
        services.AddSingleton<IConnectionFactory>(new ConnectionFactory()
        {
            HostName = publisherOptions.Host,
            UserName = publisherOptions.Username,
            Password = publisherOptions.Password
        });
        services.AddTransient(sp => ActivatorUtilities.CreateInstance<ExchangeInitializer>(sp, publisherOptions.BaseExchangeName));
        return services;
    }
    
    /// <summary>
    /// Initializes RabbitMQ exchanges using the registered <see cref="ExchangeInitializer"/> services.
    /// </summary>
    public static async Task UseRabbitMqPublishing(this IServiceProvider serviceProvider)
    {
        var exchangeInitializers = serviceProvider.GetServices<ExchangeInitializer>();
        try
        {
            foreach (var exchangeInitializer in exchangeInitializers)
                await exchangeInitializer.Initialize();
        } 
        catch (Exception e)
        {
            throw new InvalidOperationException("Failed to initialize exchanges", e);
        }
    }

    /// <summary>
    /// <para>
    /// Registers a RabbitMQ event publisher for a specific aggregate type.
    /// </para>
    /// <para>
    /// The publisher must inherit / specialize <see cref="RabbitMqEventPublisher{TAggregate}"/>
    /// </para>
    /// <para>
    /// The service is registered as <see cref="IProjector{TAggregate}"/>.
    /// The projector interface is needed to make the publisher available in the corresponding repository.
    /// </para>
    /// <para>
    /// The publisher will publish events to RabbitMQ using the configured exchange and using the event type as the routing key.<br/>
    /// <c>MyTestAggregate</c> with event <c>CreatedEvent</c> will be published with routing key <c>mytestaggregate.created.event.v1</c>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// An <see cref="EventSourcing.Mappers.ISerializationRegistry{TAggregate}"/> must be registered in the service collection for the publisher to work correctly.<br/>
    /// The simplest way to do this is to register the corresponding repository.
    /// </remarks>
    /// <remarks>
    /// The <see cref="AddRabbitMqPublishing"/> method must be called in addition to this method to ensure that RabbitMQ publishing services are available.<br/>
    /// </remarks>
    /// <param name="failOnError">Configures the publisher to return an error if publishing fails, otherwise the <see cref="RabbitMqEventPublisher{TAggregate}.ProjectAsync"/> method will always return success. (default: true).</param>
    public static IServiceCollection AddRabbitMqEventPublisher<TAggregate>(this IServiceCollection services, bool failOnError = true) where TAggregate : IAggregate
    {
        services.AddScoped<IProjector<TAggregate>, RabbitMqEventPublisher<TAggregate>>(sp => ActivatorUtilities.CreateInstance<RabbitMqEventPublisher<TAggregate>>(sp, failOnError));

        return services;
    }
    
    /// <summary>
    /// <para>
    /// Registers a RabbitMQ state publisher for a specific aggregate type.
    /// </para>
    /// <para>
    /// The publisher must inherit / specialize <see cref="RabbitMqStatePublisher{TAggregate}"/>
    /// </para>
    /// <para>
    /// The service is registered as <see cref="IProjector{TAggregate}"/>.
    /// The projector interface is needed to make the publisher available in the corresponding repository.
    /// </para>
    /// <para>
    /// The publisher will publish the current state of the aggregate to RabbitMQ using the configured exchange and using the aggregate type as the routing key.<br/>
    /// <c>MyTestAggregate</c> will be published with routing key <c>mytestaggregate.state</c>.
    /// </para>
    /// </summary>
    /// <param name="failOnError">Configures the publisher to return an error if publishing fails, otherwise the <see cref="RabbitMqStatePublisher{TAggregate}.ProjectAsync"/> method will always return success. (default: true).</param>
    /// <remarks>
    /// The <see cref="AddRabbitMqPublishing"/> method must be called in addition to this method to ensure that RabbitMQ publishing services are available.<br/>
    /// </remarks>
    public static IServiceCollection AddRabbitMqStatePublisher<TAggregate>(this IServiceCollection services, bool failOnError = true) where TAggregate : IAggregate
    {
        services.AddScoped<IProjector<TAggregate>, RabbitMqStatePublisher<TAggregate>>(sp => ActivatorUtilities.CreateInstance<RabbitMqStatePublisher<TAggregate>>(sp, failOnError));

        return services;
    }
}