using System.Reflection;
using EventSourcing;
using EventSourcing.Contexts;
using EventSourcing.Mappers;
using EventSourcing.Projections;
using EventSourcing.Repositories;
using EventSourcing.Stores;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public class EventSourcingOptionsBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<Action<IServiceCollection>> _extensions = new();
    private readonly EventMappingOptionsBuilder _eventMappingOptionsBuilder;
    private readonly EventProjectionOptionsBuilder _eventProjectionOptionsBuilder;
    
    private Action<DbContextOptionsBuilder>? _dbContextOptionsBuilderAction;
    private bool _mappingConfigured;
    private bool _projectionsConfigured;
    
    public EventSourcingOptionsBuilder(IServiceCollection services)
    {
        _services = services;
        _eventMappingOptionsBuilder = new EventMappingOptionsBuilder(services);
        _eventProjectionOptionsBuilder = new EventProjectionOptionsBuilder(services);
    }
    
    /// <summary>
    /// Configure the database context for the event store.
    /// </summary>
    /// <param name="options">An action to configure the database context with</param>
    /// <returns>The <see cref="EventSourcingOptionsBuilder"/> to be used for further configuration</returns>
    public EventSourcingOptionsBuilder ConfigureEventStoreDbContext(Action<DbContextOptionsBuilder> options)
    {
        _dbContextOptionsBuilderAction = options;
        return this;
    }
    
    /// <summary>
    /// Configure the mapping (meaning serialization and deserialization) of the events.
    /// <para>
    /// Without this configuration a <see cref="DefaultEventMapper{TEvent}"/> will be used for each event type (which inherits from <see cref="IEvent"/>) that is not covered by a custom mapper.
    /// A custom mapper is a class that implements the <see cref="IEventMapper{TEvent}"/> interface or inherits from the <see cref="AbstractEventMapper{TEvent}"/> class.
    /// </para>
    /// <para>
    /// If you configured the mapping to not use default mappers (e.g. by using <see cref="EventMappingOptionsBuilder.AddMappers(bool)"/> or <see cref="EventMappingOptionsBuilder.AddMappers(Assembly,bool)"/> with <c>registerDefaultMappers: false</c>), an exception will be thrown if there are uncovered events.
    /// (This can be ignored by calling <see cref="EventMappingOptionsBuilder.IgnoreUncoveredEvents"/>)
    /// </para>
    /// </summary>
    /// <param name="options">An action to configure the mappings with</param>
    /// <returns>The <see cref="EventSourcingOptionsBuilder"/> to be used for further configuration</returns>
    public EventSourcingOptionsBuilder ConfigureMapping(Action<EventMappingOptionsBuilder> options)
    {
        options(_eventMappingOptionsBuilder);
        _mappingConfigured = true;
        return this;
    }
    
    /// <summary>
    /// Configure the projections for the events.
    /// <para>
    /// Without this configuration all types that implement the <see cref="IProjection{TEvent}"/> interface will be registered as projections.
    /// If there were events registered in the mapping that are not covered by a projection, an exception will be thrown.
    /// (This can be ignored by calling <see cref="EventProjectionOptionsBuilder.IgnoreUncoveredEvents"/>)
    /// </para>
    /// </summary>
    /// <param name="options">An action to configure the projections with</param>
    /// <returns>The <see cref="EventSourcingOptionsBuilder"/> to be used for further configuration</returns>
    public EventSourcingOptionsBuilder ConfigureProjections(Action<EventProjectionOptionsBuilder> options)
    {
        options(_eventProjectionOptionsBuilder);
        _projectionsConfigured = true;
        return this;
    }
    
    // TODO: The Builder must be extendable to add things like the RabbitMQ publisher. There should be a method that could be used by the extension to add services etc. to the builder, so that the builder can be used to build the services.
    public EventSourcingOptionsBuilder Extend(Action<IServiceCollection> extension)
    {
        _extensions.Add(extension);
        return this;
    }
    
    public void Build()
    {
        if (_dbContextOptionsBuilderAction == null)
            throw new InvalidOperationException("The event store database context has to be configured.");
        _services.AddDbContext<IEventStoreDbContext, EventStoreDbContext>(opt => _dbContextOptionsBuilderAction?.Invoke(opt));
        _services.AddScoped<IEventStore, EventStore>();
        _services.AddScoped<IEventRepository, EventRepository>();
        _services.AddTransient<IEventBus, EventBus>();
        
        if (!_mappingConfigured)
            _eventMappingOptionsBuilder.AddMappers();
        if (!_projectionsConfigured)
            _eventProjectionOptionsBuilder.AddProjections();
        
        _eventMappingOptionsBuilder.Build();
        _eventProjectionOptionsBuilder.Build();
        
        foreach (var extension in _extensions)
            extension(_services);
    }
}