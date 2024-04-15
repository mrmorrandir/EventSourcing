using System.Reflection;
using System.Text;
using EventSourcing;
using EventSourcing.Mappers;
using EventSourcing.Projections;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
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
        _assembliesToRegisterProjections.Add(new EventProjectionAssembly(Assembly.GetCallingAssembly()));
        return this;
    }
    
    public EventProjectionOptionsBuilder AddProjections(Assembly assembly)
    {
        // TODO: For the projections there should be multiple assemblies needed - the ones with the events and the ones with the projections
        // TODO: Alternative for multiple assemblies: traversing the ServiceCollection and getting all the events... they would have to be registered as services or as IEnumerable<IEvent>
        // TODO: Idea for the projections and covered/uncovered events: get the events from the generic arguments of the IProjection<TEvent> interface and get the assembly from the type - all those assemblies must contain events.
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
            // TODO: Register the projection type as a transient service for the IEventHandler<TEvent> interface - there could be multiple projection types for the same event type
            var genericType =  typeof(IEventHandler<>).MakeGenericType(projectionType.EventType);
            _services.TryAddEnumerable(ServiceDescriptor.Transient(genericType, projectionType.Type));
            alreadyCoveredEvents.Add(projectionType.EventType);
        }
        
        var uncoveredEvents = _assembliesToRegisterProjections.SelectMany(assembly => assembly.Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.GetInterfaces().Any(i => i == typeof(IEvent)) && !alreadyCoveredEvents.Contains(t))).ToList();
        
        if (_ignoreUncoveredEvents || !uncoveredEvents.Any()) return;
        
        var uncoveredEventNames = string.Join(", ", uncoveredEvents.Select(x => x.Name));
        throw new InvalidOperationException($"There are uncovered events (in projections): {uncoveredEventNames}");
    }
}

internal class EventProjectionAssembly
{
    public Assembly Assembly { get; }

    public EventProjectionAssembly(Assembly assembly)
    {
        Assembly = assembly;
    }

}

public class EventMappingOptionsBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<Type> _mappersToRegister = new();
    private readonly List<EventMapperAssembly> _assembliesToRegisterMappers = new();
    private bool _ignoreUncoveredEvents = false;

    public EventMappingOptionsBuilder(IServiceCollection services)
    {
        _services = services;
    }
    
    public EventMappingOptionsBuilder AddMappers(bool registerDefaultMappers = true)
    {
        _assembliesToRegisterMappers.Add(new EventMapperAssembly(Assembly.GetCallingAssembly(), registerDefaultMappers));
        return this;
    } 
    
    public EventMappingOptionsBuilder AddMappers(Assembly assembly, bool registerDefaultMappers = true)
    {
        _assembliesToRegisterMappers.Add(new EventMapperAssembly(assembly, registerDefaultMappers));
        return this;
    }
    
    public EventMappingOptionsBuilder AddMapper<TEventMapper>() where TEventMapper : class, IEventMapper
    {
        _mappersToRegister.Add(typeof(TEventMapper));
        return this;
    }
    
    public EventMappingOptionsBuilder IgnoreUncoveredEvents()
    {
        _ignoreUncoveredEvents = true;
        return this;
    }

    public void Build()
    {
        var eventMappers = new List<EventMapperType> ();
        foreach (var assembly in _assembliesToRegisterMappers)
        {
            // Get all classes that implement the IEventMapper<TEvent> interface
            eventMappers.AddRange(assembly.Assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false } && t.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventMapper<>)))
                .Select(t => new EventMapperType(t,
                    t.GetInterfaces()
                        .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventMapper<>))
                        .GetGenericArguments()[0]))
                .ToList());
        }

        var eventTypesWithMultipleMappers = eventMappers.GroupBy(x => x.EventType).Where(x => x.Count() > 1).ToArray();
        if (eventTypesWithMultipleMappers.Any())
        {
            var exceptionMessage = new StringBuilder($"There are multiple custom event mappers for the same event type:");
            foreach (var group in eventTypesWithMultipleMappers)
                exceptionMessage.AppendLine($"- Event: {group.Key.Name} => Mappers: {string.Join(", ", group.Select(x => x.Type.Name))}");
            throw new InvalidOperationException(exceptionMessage.ToString());
        }
        
        foreach (var eventMapper in eventMappers)
            _services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IEventMapper), eventMapper.Type));

        var alreadyCoveredEvents = eventMappers.Select(mapper => mapper.EventType).ToList();
        foreach (var assembly in _assembliesToRegisterMappers.Where(a => a.RegisterDefaultMappers)) 
        {
            // Get all classes and records that implement the non-generic IEvent interface
            var eventTypesInAssembly = assembly.Assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false } && t.GetInterfaces().Any(i => i == typeof(IEvent)))
                .ToList();

            foreach (var eventType in eventTypesInAssembly)
            {
                if (alreadyCoveredEvents.Contains(eventType)) continue;
                
                var defaultEventMapperType = typeof(DefaultEventMapper<>).MakeGenericType(eventType);
                _services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IEventMapper), defaultEventMapperType));
                alreadyCoveredEvents.Add(eventType);
            }
        }

        foreach (var mapper in _mappersToRegister)
        {
            // Get the event type that the mapper is for
            var eventType = mapper.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventMapper<>))
                .GetGenericArguments()[0];
            if (alreadyCoveredEvents.Contains(eventType))
                throw new InvalidOperationException($"The mapper {mapper.Name} cannot be registered by the AddMapper<TEventMapper>() method.\nThere is already an event mapper for the event type {eventType.Name}.");
            _services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IEventMapper), mapper));
            alreadyCoveredEvents.Add(eventType);
        }

        var uncoveredEvents = _assembliesToRegisterMappers.SelectMany(assembly => assembly.Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.GetInterfaces().Any(i => i == typeof(IEvent)) && !alreadyCoveredEvents.Contains(t))).ToList();

        if (_ignoreUncoveredEvents || !uncoveredEvents.Any()) return;

        var uncoveredEventNames = string.Join(", ", uncoveredEvents.Select(x => x.Name));
        throw new InvalidOperationException($"There are uncovered events (in mappings): {uncoveredEventNames}");
    }
}