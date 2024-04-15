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

internal class ProjectionType
{
    public Type Type { get; }
    public Type InterfaceType { get; }
    public Type EventType { get; }

    public ProjectionType(Type type,Type interfaceType, Type eventType)
    {
        Type = type;
        InterfaceType = interfaceType;
        EventType = eventType;
    }
}