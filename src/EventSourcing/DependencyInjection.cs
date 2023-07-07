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

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddEventSourcing(this IServiceCollection services, Action<DbContextOptionsBuilder>? options)
    {
        services.AddDbContext<IEventStoreDbContext, EventStoreDbContext>(opt => options?.Invoke(opt));
        services.AddScoped<IEventStore, EventStore>();
        services.AddScoped<IEventRepository, EventRepository>();
        
        services.AddTransient<IEventBus, EventBus>();
        return services;
    }

    public static IServiceCollection AddProjections(this IServiceCollection services, Assembly assembly)
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
    
    public static IServiceCollection AddEventMappers(this IServiceCollection services, Action<EventMapperConfig> configFunc)
    {
        var configuration = new EventMapperConfig();
        configFunc(configuration);

        foreach (var assembly in configuration.AssembliesToRegister)
            services.AddEventMappersForAssembly(assembly);
        
        services.AddScoped<IEventRegistry, EventRegistry>();
        return services;
    }

    private static IServiceCollection AddEventMappersForAssembly(this IServiceCollection services, Assembly assembly)
    {
        // Get all classes that implement the IEventMapper<TEvent> interface
        var eventMappers = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventMapper<>)))
            .ToList();
        foreach (var eventMapper in eventMappers)
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IEventMapper), eventMapper));
        return services;
    }

}

public class EventMapperConfig
{
    internal List<Assembly> AssembliesToRegister { get; }= new();

    public EventMapperConfig AddAssembly(Assembly assembly)
    {
        AssembliesToRegister.Add(assembly);
        return this;
    }
}