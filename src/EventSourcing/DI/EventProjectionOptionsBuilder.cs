using System.Reflection;
using EventSourcing;
using EventSourcing.Projections;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public class EventProjectionOptionsBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<EventProjectionAssembly> _assembliesToRegisterProjections = new();
    private bool _ignoreUncoveredEvents;

    public EventProjectionOptionsBuilder(IServiceCollection services)
    {
        _services = services;
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

    public void Build()
    {
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
            _services.TryAddEnumerable(ServiceDescriptor.Transient(genericType, projectionType.Type));
            _services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IEventHandler), projectionType.Type));
            alreadyCoveredEvents.Add(projectionType.EventType);
        }
        
        if (_ignoreUncoveredEvents) return;
        var allEvents = _services.Where(descriptor => descriptor.ServiceType == typeof(IEvent)).Select(descriptor => descriptor.ImplementationType).ToList();
        var uncoveredEvents = allEvents.Except(alreadyCoveredEvents).ToList();
        if (!uncoveredEvents.Any()) return;
        var uncoveredEventNames = string.Join(", ", uncoveredEvents.Select(x => x?.FullName));
        throw new InvalidOperationException($"There are uncovered events (in projections): {uncoveredEventNames}");
    }
}