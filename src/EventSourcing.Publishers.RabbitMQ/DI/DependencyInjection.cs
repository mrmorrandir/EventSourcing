// ReSharper disable once CheckNamespace

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace EventSourcing.Publishers.RabbitMQPublisher;

public static class DependencyInjection
{
    public static IServiceCollection AddRabbitMQPublishers(this IServiceCollection services, Action<RabbitMQPublisherConfig> configAction)
    {
        var config = new RabbitMQPublisherConfig();
        configAction(config);

        foreach (var assembly in config.AssembliesToRegisterDefaultPublishers)
            services.AddDefaultPublishersForAssembly(assembly, config.BaseExchangeName);
       
        var connectionFactoryImplementation = new ConnectionFactory
        {
            HostName = config.Host,
            UserName = config.Username,
            Password = config.Password,
            DispatchConsumersAsync = true
        };
        
        services.AddSingleton<IAsyncConnectionFactory>(connectionFactoryImplementation);
        return services;
    }
    
    private static IServiceCollection AddDefaultPublishersForAssembly(this IServiceCollection services, Assembly assembly, string baseExchangeName)
    {
        var eventTypes = assembly.GetTypes()
            .Where(t => typeof(IEvent).IsAssignableFrom(t))
            .ToList();

        foreach (var eventType in eventTypes)
        {
            var eventHandlerServiceType = typeof(IEventHandler<>).MakeGenericType(eventType);
            var publisherServiceType = typeof(IPublisher<>).MakeGenericType(eventType);
            
            var publisherImplementationType = typeof(Publisher<>).MakeGenericType(eventType);
            services.AddTransient(publisherServiceType, sp => ActivatorUtilities.CreateInstance(sp, publisherImplementationType, baseExchangeName));
            services.AddTransient(eventHandlerServiceType, sp => ActivatorUtilities.CreateInstance(sp, publisherImplementationType, baseExchangeName));
        }
        
        return services;
    }
}