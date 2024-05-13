using System.Reflection;
using EventSourcing;
using EventSourcing.Projections;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public class EventProjectionOptionsBuilder
{
    private readonly List<EventProjectionAssembly> _assembliesToRegisterProjections = new();
    private bool _ignoreUncoveredEvents;

    public EventProjectionOptionsBuilder()
    {
    }
    
    public EventProjectionOptionsBuilder AddProjections()
    {
        _assembliesToRegisterProjections.Add(new EventProjectionAssembly(Assembly.GetEntryAssembly()!));
        return this;
    }
    
    public EventProjectionOptionsBuilder AddProjections(Assembly assembly)
    {
        _assembliesToRegisterProjections.Add(new EventProjectionAssembly(assembly));
        return this;
    }

    public EventProjectionOptionsBuilder IgnoreUncoveredEvents()
    {
        _ignoreUncoveredEvents = true;
        return this;
    }

    public EventProjectionOptions Build(IEnumerable<Type>? recentlyFoundEvents = null)
    {
        var services = new ServiceCollection();
        var projectionTypes = new List<ProjectionType>();
        foreach (var assembly in _assembliesToRegisterProjections)
        {
            var projectionTypesInAssembly = assembly.Assembly.GetTypes()
                .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjection<>)))
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjection<>))
                    .Select(projectionInterface => new ProjectionType(t, projectionInterface, projectionInterface.GetGenericArguments()[0])))
                .ToList();
            projectionTypes.AddRange(projectionTypesInAssembly);
        }   
        
        var alreadyCoveredEvents = new List<Type>();
        foreach (var projectionType in projectionTypes)
        {
            var genericType =  typeof(IEventHandler<>).MakeGenericType(projectionType.EventType);
            services.TryAddEnumerable(ServiceDescriptor.Transient(genericType, projectionType.Type));
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IEventHandler), projectionType.Type));
            alreadyCoveredEvents.Add(projectionType.EventType);
        }
        
        var uncoveredEvents = (recentlyFoundEvents ?? new List<Type>()).Except(alreadyCoveredEvents).ToList();
        if (_ignoreUncoveredEvents || !uncoveredEvents.Any())
            return new EventProjectionOptions(services, alreadyCoveredEvents, uncoveredEvents);

        var uncoveredEventNames = string.Join(", ", uncoveredEvents.Select(x => x?.FullName));
        throw new InvalidOperationException($"There are uncovered events (in projections): {uncoveredEventNames}");
    }
}