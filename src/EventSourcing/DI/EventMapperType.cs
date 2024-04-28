// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

internal class EventMapperType
{
    public Type Type { get; }
    public Type EventType { get; }

    public EventMapperType(Type type, Type eventType)
    {
        Type = type;
        EventType = eventType;
    }
}