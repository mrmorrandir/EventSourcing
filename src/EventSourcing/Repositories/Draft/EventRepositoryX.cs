using System.Reflection;
using EventSourcing.Abstractions;
using EventSourcing.Abstractions.Mappers;
using EventSourcing.Abstractions.Repositories;
using EventSourcing.Abstractions.Stores;
using EventSourcing.Mappers;
using EventSourcing.Stores;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventSourcing.Repositories;

public class EventRepositoryX : IEventRepository
{
    private readonly IEventStore _eventStore;
    private readonly IEventRegistry _eventMapper;
    private readonly IEventBus _eventBus;

    public EventRepositoryX(IEventStore eventStore, IEventRegistry eventMapper, IEventBus eventBus)
    {
        _eventStore = eventStore;
        _eventMapper = eventMapper;
        _eventBus = eventBus;
    }
    
    public async Task<Result<TAggregate>> GetAsync<TAggregate>(Guid id, CancellationToken cancellationToken = default)  where TAggregate : IAggregateRoot
    {
        IEnumerable<IEventData> eventDataHistory;
        try
        {
            eventDataHistory = await _eventStore.GetAsync(id, cancellationToken);
        }
        catch (Exception e)
        {
            return Result.Fail<TAggregate>($"Error while getting process with id {id} from event store: {e.Message}");
        }

        var eventHistory = new List<IEvent>();
        foreach (var eventData in eventDataHistory)
        {
            try
            {
                var @event = _eventMapper.Deserialize(eventData.Type, eventData.Data);
                eventHistory.Add(@event);
            }
            catch (Exception ex)
            {
                return Result.Fail<TAggregate>(ex.Message);
            }
        }
        
        var aggregate = (TAggregate)Activator.CreateInstance(typeof(TAggregate), true)!;
        aggregate.FromHistory(eventHistory);
        return Result.Ok(aggregate);
    }
    
    public async Task<Result> SaveAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default) where TAggregate : IAggregateRoot
    {
        var changes = aggregate.GetChanges();
        if (!changes.Any()) return Result.Ok();
        
        var eventStoreEvents = new List<EventData>();
        var streamId = aggregate.Id;
        var streamVersion = changes.First().ExpectedVersion;

        try
        {
            foreach (var change in changes)
            {
                var serializedEvent = _eventMapper.Serialize(change.Event);
                var eventData = new EventData
                {
                    Id = Guid.NewGuid(),
                    Created = DateTime.Now,
                    StreamId = aggregate.Id,
                    Version = change.ExpectedVersion + 1,
                    Type = serializedEvent.Type,
                    Data = serializedEvent.Data
                };

                eventStoreEvents.Add(eventData);
            }
        }
        catch (Exception e)
        {
            return Result.Fail($"Error while mapping events to event store events: {e.Message}");
        }

        try 
        {
            await _eventStore.AppendAsync(streamId, streamVersion, eventStoreEvents, cancellationToken);
        }
        catch (Exception e)
        {
            return Result.Fail($"Error while saving process with id {aggregate.Id} to event store: {e.Message}");
        }
        
        aggregate.ClearChanges();

        foreach (var change in changes)
        {
            try {
                await _eventBus.PublishAsync(change.Event, cancellationToken);
            }
            catch (Exception e)
            {
                return Result.Fail($"Error while publishing event {change.Event.GetType().Name} to event bus: {e.Message}");
            }
        }
        return Result.Ok();
    }
}

public interface IEventRegistry 
{
    ISerializedEvent Serialize(IEvent @event);
    IEvent Deserialize(string type, string data);
}

public class EventRegistry : IEventRegistry
{
    private readonly IEnumerable<IEventMapperX> _eventMappers;
    private readonly ILookup<string, Func<string, string, IEvent>> _deserializerLookup;
    private readonly ILookup<Type, Func<IEvent, ISerializedEvent>> _serializerLookup;

    public EventRegistry(IEnumerable<IEventMapperX> eventMappers)
    {
        _eventMappers = eventMappers;
        // TODO: Check for doublicate "Types" in eventMappers (the string as well as the event type)
        _serializerLookup = _eventMappers
            .ToLookup(
                eventMapper => eventMapper.EventType, 
                eventMapper =>
                {
                    var serializeMethod = eventMapper.GetType().GetMethod("Serialize")!;
                    var serializeDelegate = (Func<IEvent, ISerializedEvent>)(@event => (ISerializedEvent)serializeMethod.Invoke(eventMapper, new object[] { @event }));
                    return serializeDelegate;
                });
        _deserializerLookup = _eventMappers
            .SelectMany(em => em.Types.Select(t => new { Type = t, Mapper = em }))
            .ToLookup(
                typeAndMapper => typeAndMapper.Type, 
                typeAndMapper =>
                {
                    var deserializeMethod = typeAndMapper.Mapper.GetType().GetMethod("Deserialize")!;
                    var deserializeDelegate = (Func<string, string, IEvent>)((type, data) => (IEvent)deserializeMethod.Invoke(typeAndMapper.Mapper, new object[] { type, data }));
                    return deserializeDelegate;
                });
    }

    public ISerializedEvent Serialize(IEvent @event)
    {
        var serializer = _serializerLookup[@event.GetType()].FirstOrDefault();
        if (serializer is null)
            throw new EventRegistryException($"No event mapper found for event {@event.GetType().Name}");
        
        return serializer(@event);
    }

    public IEvent Deserialize(string type, string data)
    {
        var deserializer = _deserializerLookup[type].FirstOrDefault();
        if (deserializer is null)
            throw new EventRegistryException($"No event mapper found for event type {type}");
        
        return deserializer(type, data);
    }
}

public class EventRegistryException : Exception
{
    public EventRegistryException() { }
    public EventRegistryException(string message) : base(message) { }
    public EventRegistryException(string message, Exception inner) : base(message, inner) { }
}

public static class DependencyInjection
{
    public static IServiceCollection AddEventMappers(this IServiceCollection services, Assembly assembly)
    {
        // Get all classes that implement the IEventMapperX<TEvent> interface
        var eventMappers = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventMapperX<>)))
            .ToList();
        foreach (var eventMapper in eventMappers)
            services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IEventMapperX), eventMapper));

        services.AddSingleton<IEventRegistry, EventRegistry>();
        services.AddSingleton<IEventRepository, EventRepositoryX>();
        return services;
    }
}