using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

internal class EventMapperAssembly
{
    public Assembly Assembly { get; }
    public bool RegisterDefaultMappers { get; }

    public EventMapperAssembly(Assembly assembly, bool registerDefaultMappers)
    {
        Assembly = assembly;
        RegisterDefaultMappers = registerDefaultMappers;
    }
}