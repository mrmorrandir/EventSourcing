using System.Reflection;
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
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing;

public static class DependencyInjection
{
    public static IServiceCollection AddEventSourcing(this IServiceCollection services, Action<DbContextOptionsBuilder>? options)
    {
        services.AddDbContext<IEventStoreDbContext, EventStoreDbContext>(opt => options?.Invoke(opt));
        services.AddScoped<IEventStore, EventStore>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddSingleton<IEventMapper, EventMapper>();
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
    
    public static IServiceProvider AddEventMappings(this IServiceProvider serviceProvider, Action<IEventMapper> configure)
    {
        var mapper = serviceProvider.GetRequiredService<IEventMapper>();
        configure(mapper);
        return serviceProvider;
    }
}