using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

internal class EventProjectionAssembly
{
    public Assembly Assembly { get; }

    public EventProjectionAssembly(Assembly assembly)
    {
        Assembly = assembly;
    }

}