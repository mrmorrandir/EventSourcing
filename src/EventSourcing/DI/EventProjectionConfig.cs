using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public class EventProjectionConfig
{
    internal List<Assembly> AssembliesToRegisterProjections { get; }= new();
    
    /// <inheritdoc cref="DependencyInjection.AddProjectionsFromAssembly"/>
    public EventProjectionConfig AddProjections(Assembly assembly)
    {
        AssembliesToRegisterProjections.Add(assembly);
        return this;
    }
}