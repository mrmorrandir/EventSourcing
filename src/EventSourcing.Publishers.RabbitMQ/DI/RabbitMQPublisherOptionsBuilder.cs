using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQ.Client;

namespace EventSourcing.Publishers.RabbitMQPublisher;

public class RabbitMQPublisherOptionsBuilder
{
    private readonly List<PublisherAssembly> _assembliesToRegisterPublishers = new();
    private readonly List<PublisherEvent> _eventsToRegisterPublisher = new();
    private readonly IServiceCollection _services;
    private string _host = "localhost";
    private string _password = "guest";
    private string _username = "guest";
    private string _baseExchangeName = "events";

    public RabbitMQPublisherOptionsBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public RabbitMQPublisherOptionsBuilder UseConnection(string host, string username, string password)
    {
        _host = host;
        _username = username;
        _password = password;
        return this;
    }
    
    public RabbitMQPublisherOptionsBuilder UseBaseExchangeName(string baseExchangeName)
    {
        _baseExchangeName = baseExchangeName;
        return this;
    }

    public RabbitMQPublisherOptionsBuilder AddPublishers(Assembly assembly, string? baseExchangeName = null)
    {
        _assembliesToRegisterPublishers.Add(new PublisherAssembly(assembly, baseExchangeName));
        return this;
    }
    
    public RabbitMQPublisherOptionsBuilder AddPublisher<TEvent>(string? baseExchangeName = null) where TEvent : IEvent
    {
        _eventsToRegisterPublisher.Add(new PublisherEvent(typeof(TEvent), baseExchangeName));
        return this;
    }

    public void Build()
    {
        foreach (var assembly in _assembliesToRegisterPublishers)
        {
            var eventTypesInAssembly = assembly.Assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false } && t.GetInterfaces().Any(i => i == typeof(IEvent)))
                .ToList();
            foreach (var eventType in eventTypesInAssembly)
            {
                // Each publisher is registered as a publisher and an event handler
                // - the event handler registration will be used by the event bus to publish the events
                // - the publisher registration can be used to publish events manually
                var eventHandlerServiceType = typeof(IEventHandler<>).MakeGenericType(eventType);
                var publisherServiceType = typeof(IPublisher<>).MakeGenericType(eventType);

                var publisherImplementationType = typeof(Publisher<>).MakeGenericType(eventType);
                _services.TryAddEnumerable(ServiceDescriptor.Transient(publisherServiceType, sp => ActivatorUtilities.CreateInstance(sp, publisherImplementationType, assembly.BaseExchangeName ?? _baseExchangeName)));
                _services.TryAddEnumerable(ServiceDescriptor.Transient(eventHandlerServiceType, sp => ActivatorUtilities.CreateInstance(sp, publisherImplementationType, assembly.BaseExchangeName ?? _baseExchangeName)));
            }
        }

        foreach (var publisherEvent in _eventsToRegisterPublisher)
        {
            var eventHandlerServiceType = typeof(IEventHandler<>).MakeGenericType(publisherEvent.EventType);
            var publisherServiceType = typeof(IPublisher<>).MakeGenericType(publisherEvent.EventType);

            var publisherImplementationType = typeof(Publisher<>).MakeGenericType(publisherEvent.EventType);
            _services.AddTransient(publisherServiceType, sp => ActivatorUtilities.CreateInstance(sp, publisherImplementationType, publisherEvent.BaseExchangeName ?? _baseExchangeName));
            _services.AddTransient(eventHandlerServiceType, sp => ActivatorUtilities.CreateInstance(sp, publisherImplementationType, publisherEvent.BaseExchangeName ?? _baseExchangeName));
        }

        var connectionFactoryImplementation = new ConnectionFactory
        {
            HostName = _host,
            UserName = _username,
            Password = _password,
            DispatchConsumersAsync = true
        };

        _services.AddSingleton<IAsyncConnectionFactory>(connectionFactoryImplementation);
    }
}