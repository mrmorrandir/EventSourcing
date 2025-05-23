using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using EventSourcing;
using EventSourcing.DI;
using EventSourcing.Mappers;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public class EventMappingOptionsBuilder
{
    private EventMapperAssembly? _assemblyToRegisterMappers;

    public EventMappingOptionsBuilder()
    {
    }
    
    public EventMappingOptionsBuilder AddMappers(Assembly assembly)
    {
        _assemblyToRegisterMappers = new EventMapperAssembly(assembly, true);
        return this;
    }

    public EventMappingOptions Build()
    {
        if (_assemblyToRegisterMappers is null)
            throw new InvalidOperationException("No assembly to register mappers was provided.");
        
        var services = new ServiceCollection();
        // find the "EventSourcing.Generated.EventSourcing" class in the assembly
        var eventRegistryType = _assemblyToRegisterMappers.Assembly.GetTypes().FirstOrDefault(t => t.IsClass && t is { IsPublic: true, Name: "EventRegistry", Namespace: "EventSourcing.Generated" });
        if (eventRegistryType == null)
            throw new InvalidOperationException($"Could not find the EventSourcing.Generated.EventSourcing class in the assembly {_assemblyToRegisterMappers.Assembly.FullName}");
        
        // find all classes or records that implement the "IEvent" interface - because those are the ones that will be covered by the source-generated EventRegistry
        var coveredEvents = _assemblyToRegisterMappers.Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.GetInterfaces().Any(i => i == typeof(IEvent)))
            .ToImmutableArray();

        
        return new EventMappingOptions(new ServiceCollection().AddSingleton(typeof(IEventRegistry), eventRegistryType), coveredEvents);
    }
}