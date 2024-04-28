// ReSharper disable once CheckNamespace
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Publishers.RabbitMQPublisher;

public static class DependencyInjection
{
    public static EventSourcingOptionsBuilder AddRabbitMQPublishing(this EventSourcingOptionsBuilder builder, Action<RabbitMQPublisherOptionsBuilder> options)
    {
        builder.Extend(services =>
        {
            var rabbitMqPublisherOptionsBuilder = new RabbitMQPublisherOptionsBuilder(services);
            options(rabbitMqPublisherOptionsBuilder);
            rabbitMqPublisherOptionsBuilder.Build();
        });
        
        return builder;
    }
}