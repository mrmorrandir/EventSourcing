using System.Reflection;

namespace EventSourcing.Publishers.RabbitMQPublisher;

public class PublisherAssembly
{
    public Assembly Assembly { get; }
    public string? BaseExchangeName { get; }
    
    public PublisherAssembly(Assembly assembly, string? baseExchangeName)
    {
        Assembly = assembly;
        BaseExchangeName = baseExchangeName;
    }
}