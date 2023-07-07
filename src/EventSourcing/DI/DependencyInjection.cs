using System.Reflection;
using EventSourcing;
using EventSourcing.Abstractions;
using EventSourcing.Abstractions.Mappers;
using EventSourcing.Abstractions.Projections;
using EventSourcing.Abstractions.Repositories;
using EventSourcing.Abstractions.Stores;
using EventSourcing.Contexts;
using EventSourcing.Mappers;
using EventSourcing.Repositories;
using EventSourcing.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    /// <summary>
    /// <para>
    /// Registers the requires services to use the event sourcing framework.
    /// </para>
    /// <para>
    /// The following services are registered:<br/>
    /// The <see cref="IEventRepository"/> interface for querying the event store - you will only work with that most of the time.<br/>
    /// The <see cref="IEventBus"/> interface for publishing events - you can work with that, but you'll probably never do (the repository makes use of it to publish the events for projections etc.).<br/>
    /// The <see cref="IEventStore"/> interface for persistence - it is only meant to be used internal by the repository.<br/>
    /// The <see cref="IEventStoreDbContext"/> interface for the database context - you should leave that alone, but you can configure it in the <paramref name="options"/>.
    /// </para>
    /// <para>
    /// In order to use the event sourcing framework, you also have to register some event mappers. You can do that with the <see cref="AddEventMappers"/> method.
    /// </para>
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for the dependency injection.</param>
    /// <param name="options">An action to configure the database with.</param>
    /// <returns>The <see cref="IServiceCollection"/> for the dependency injection.</returns>
    public static IServiceCollection AddEventSourcing(this IServiceCollection services, Action<DbContextOptionsBuilder>? options)
    {
        services.AddDbContext<IEventStoreDbContext, EventStoreDbContext>(opt => options?.Invoke(opt));
        services.AddScoped<IEventStore, EventStore>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddTransient<IEventBus, EventBus>();
        return services;
    }

    /// <summary>
    /// Registers projections depending on the <see cref="EventProjectionConfig"/> configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for the dependency injection.</param>
    /// <param name="configFunc">The action to configure the projections with.</param>
    /// <returns>The <see cref="IServiceCollection"/> for the dependency injection.</returns>
    public static IServiceCollection AddProjections(this IServiceCollection services, Action<EventProjectionConfig> configAction)
    {
        var configuration = new EventProjectionConfig();
        configAction(configuration);
        
        foreach (var assembly in configuration.AssembliesToRegisterProjections)
            services.AddProjectionsFromAssembly(assembly);
        
        return services;
    }

    /// <summary>
    /// Registers all classes that implement <see cref="IProjection{TEvent}"/> from the specified <paramref name="assembly"/>.
    /// <para>
    /// The <see cref="IProjection{TEvent}"/> interface is implemented by classes that project events to a read model.<br/>
    /// The <see cref="ServiceDescriptor"/> type <see cref="IEventHandler{TEvent}"/> is used to register the projections (<see cref="IProjection{TEvent}"/> inherits from <see cref="IEventHandler{TEvent}"/> - which is used by the <see cref="IEventBus"/>).<br/>
    /// </para>
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for the dependency injection.</param>
    /// <param name="assembly">The assembly to analyze</param>
    /// <returns>The <see cref="IServiceCollection"/> for the dependency injection.</returns>
    private static IServiceCollection AddProjectionsFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var projectionTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjection<>)))
            .ToList();
        foreach (var projectionType in projectionTypes)
        {
            var projectionInterfaces = projectionType.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProjection<>));
            foreach(var projectionInterface in projectionInterfaces)
            {
                var genericType = typeof(IEventHandler<>).MakeGenericType(projectionInterface.GetGenericArguments().First());
                services.AddTransient(genericType, projectionType);
            }
        }

        return services;
    }
    
    /// <summary>
    /// Registers custom event mappers and default event mappers depending on the <see cref="EventMapperConfig"/> configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for the dependency injection.</param>
    /// <param name="configFunc">The action to configure the mappers with.</param>
    /// <returns>The <see cref="IServiceCollection"/> for the dependency injection.</returns>
    public static IServiceCollection AddEventMappers(this IServiceCollection services, Action<EventMapperConfig> configFunc)
    {
        var configuration = new EventMapperConfig();
        configFunc(configuration);

        foreach (var assembly in configuration.AssembliesToRegisterCustomMappers)
            services.AddCustomEventMappersFromAssembly(assembly);

        foreach (var assembly in configuration.AssembliesToRegisterDefaultMappers)
            services.AddDefaultEventMappersFromAssembly(assembly);
        
        foreach (var customMapper in configuration.CustomMappersToRegister)
            services.AddCustomEventMapper(customMapper);
        
        services.AddScoped<IEventRegistry, EventRegistry>();
        return services;
    }

    private static IServiceCollection AddCustomEventMapper(this IServiceCollection services, Type mapperType)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IEventMapper), mapperType));
        return services;
    }

    /// <summary>
    /// Registers all classes that implement the <see cref="IEventMapper{TEvent}"/> interface in the specified assembly.
    /// <para>
    /// The <see cref="ServiceDescriptor"/> type <see cref="IEventMapper"/> is used to register the event mappers as enumerable IEnumerable&lt;IEventMapper&gt;.
    /// </para>
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for the dependency injection.</param>
    /// <param name="assembly">The assembly to analyze</param>
    /// <returns>The <see cref="IServiceCollection"/> for the dependency injection.</returns>
    private static IServiceCollection AddCustomEventMappersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        // Get all classes that implement the IEventMapper<TEvent> interface
        var customEventMapperTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventMapper<>)))
            .ToList();
        
        var eventTypeGroups = customEventMapperTypes.GroupBy(mapperType => mapperType.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventMapper<>)).GetGenericArguments()[0]).ToList();
        if (eventTypeGroups.Any(group => group.Count() > 1))
        {
            var firstEventTypeName = eventTypeGroups.First(group => group.Count() > 1).Key.Name;
            var firstEventTypeMappers = string.Join(", ", eventTypeGroups.First(group => group.Count() > 1).Select(type => type.Name));
            throw new InvalidOperationException($"There are multiple custom event mappers for the same event type. (Event type: {firstEventTypeName} / Event mappers: {firstEventTypeMappers})");
        }

        foreach (var eventMapper in customEventMapperTypes)
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IEventMapper), eventMapper));
        return services;
    }
    
    /// <summary>
    /// Registers a <see cref="DefaultEventMapper{TEvent}"/> for each event type that does not have a custom event mapper.
    /// <para>
    /// The <see cref="ServiceDescriptor"/> type <see cref="IEventMapper"/> is used to register the event mappers as enumerable IEnumerable&lt;IEventMapper&gt;.<br/>
    /// All events for that custom mapper classes exist (the ones that implement the <see cref="IEventMapper{TEvent}"/> interface) in the specified assembly are ignored.
    /// </para>
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for the dependency injection.</param>
    /// <param name="assembly">The assembly to analyze</param>
    /// <returns>The <see cref="IServiceCollection"/> for the dependency injection.</returns>
    private static IServiceCollection AddDefaultEventMappersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        // Get all classes and records that implement the non-generic IEvent interface
        var eventTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i => i == typeof(IEvent)))
            .ToList();

        var customEventMapperEventTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventMapper<>)))
            .Select(mapperType => mapperType.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventMapper<>)).GetGenericArguments()[0]).ToList();
        
        foreach (var eventType in eventTypes)
        {
            if (!customEventMapperEventTypes.Contains(eventType)) {
                var defaultEventMapperType = typeof(DefaultEventMapper<>).MakeGenericType(eventType);
                services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IEventMapper), defaultEventMapperType));
            }
        }

        return services;
    }
}