
using EventSourcing.Publishers.RabbitMQ;
using EventSourcing.Publishers.RabbitMQ.DI;
// ReSharper disable once CheckNamespace
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Publishers.RabbitMQPublisher;

public static class DependencyInjection
{
    public static EventSourcingOptionsBuilder AddRabbitMqPublishing(this EventSourcingOptionsBuilder builder, Action<RabbitMQPublisherOptionsBuilder> options)
    {
        builder.Extend(services =>
        {
            var rabbitMqPublisherOptionsBuilder = new RabbitMQPublisherOptionsBuilder(services);
            options(rabbitMqPublisherOptionsBuilder);
            rabbitMqPublisherOptionsBuilder.Build();
        });
        
        return builder;
    }
    
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
}