using EventSourcing;
using EventSourcing.Contexts;
using EventSourcing.Stores;
using EventSourcing.Repositories;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    /// <summary>
    /// <para>
    /// Registers the required services to use the event sourcing framework.
    /// </para>
    /// <para>
    /// The following services are registered:<br/>
    /// The <see cref="IEventRepository"/> interface for querying the event store - you will only work with that most of the time.<br/>
    /// The <see cref="IEventBus"/> interface for publishing events - you can work with that, but you'll probably never do (the repository makes use of it to publish the events for projections etc.).<br/>
    /// The <see cref="IEventStore"/> interface for persistence - it is only meant to be used internal by the repository.<br/>
    /// The <see cref="IEventStoreDbContext"/> interface for the database context - you should leave that alone, but you can configure it in the <paramref name="options"/>.
    /// </para>
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for the dependency injection.</param>
    /// <param name="options">An action to configure the database with.</param>
    /// <returns>The <see cref="IServiceCollection"/> for the dependency injection.</returns>
    public static IServiceCollection AddEventSourcing(this IServiceCollection services, Action<EventSourcingOptionsBuilder>? options)
    {
        var builder = new EventSourcingOptionsBuilder(services);
        options?.Invoke(builder);
        builder.Build();
        return services;
    }
}