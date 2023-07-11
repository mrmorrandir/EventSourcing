using System.Reflection;
using EventSourcing.Abstractions;

namespace EventSourcing.Publishers.RabbitMQPublisher;

public class RabbitMQPublisherConfig
{
    internal List<Assembly> AssembliesToRegisterDefaultPublishers { get; } = new();

    public string Host { get; set; } = "localhost";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string BaseExchangeName { get; set; } = "events";
    
    public RabbitMQPublisherConfig AddDefaultPublishersForAssembly(Assembly assembly)
    {
        AssembliesToRegisterDefaultPublishers.Add(assembly);
        return this;
    }
}