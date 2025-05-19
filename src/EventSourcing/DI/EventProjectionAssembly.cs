using System.Reflection;

namespace EventSourcing.DI;

internal class EventProjectionAssembly
{
    public Assembly Assembly { get; }

    public EventProjectionAssembly(Assembly assembly)
    {
        Assembly = assembly;
    }

}