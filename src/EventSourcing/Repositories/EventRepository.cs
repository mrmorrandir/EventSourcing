using EventSourcing.Mappers;
using EventSourcing.Stores;
using FluentResults;

namespace EventSourcing.Repositories;

public class EventRepository : IEventRepository
{
    private readonly IEventStore _eventStore;
    private readonly IEventRegistry _eventMapper;
    private readonly IEventBus _eventBus;

    public EventRepository(IEventStore eventStore, IEventRegistry eventMapper, IEventBus eventBus)
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