using EventSourcing;
using EventSourcing.Contexts;
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
    
    public EventSourcingOptionsBuilder ConfigureEventStoreDbContext(Action<DbContextOptionsBuilder> options)
    {
        _dbContextOptionsBuilderAction = options;
        return this;
    }
    
    public EventSourcingOptionsBuilder ConfigureMapping(Action<EventMappingOptionsBuilder> options)
    {
        options(_eventMappingOptionsBuilder);
        _mappingConfigured = true;
        return this;
    }
    
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