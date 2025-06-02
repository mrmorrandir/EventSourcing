using EventSourcing.Contexts;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.Stores;

public class EventStore : IEventStore
{
    private readonly IEventStoreDbContext _eventStoreDbContext;

    public EventStore(IEventStoreDbContext eventStoreDbContext)
    {
        _eventStoreDbContext = eventStoreDbContext;
    }

    public async Task<IEnumerable<IEventEntity>> GetAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await _eventStoreDbContext.Events.AsNoTracking()
                .Where(e => e.StreamId == streamId)
                .OrderBy(e => e.Version)
                .ToListAsync(cancellationToken);
            if (events.Count <= 0)
                throw new EventStoreException($"Stream with id {streamId} not found");
            
            return events;
        }
        catch (EventStoreException)
        {
            throw;
        }
        catch (Exception ex)
        {
           throw new EventStoreException($"Failed to get events for stream with id '{streamId}'.", ex);
        }
    }

    public async Task AppendAsync(Guid streamId, int expectedVersion, IEnumerable<IEventEntity> events, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await _eventStoreDbContext.Events.AsNoTracking().Where(x => x.StreamId == streamId).MaxAsync(x => x.Version, cancellationToken: cancellationToken) == expectedVersion)
                throw new EventStoreException($"Stream with id {streamId} has been modified - expected version {expectedVersion} but found higher version");
            
            var eventList = events.ToList();
            if (!eventList.Any())
                throw new EventStoreException("No events to append.");
            
            // check all events have the same stream id
            if (eventList.Any(e => e.StreamId != streamId))
                throw new EventStoreException($"All events must have the same stream id '{streamId}'.");
            
            // check that all DateTimeOffset values are in UTC
            if (eventList.Any(e => e.Created.Offset != TimeSpan.Zero))
                throw new EventStoreException("All event creation timestamps must be in UTC.");
            
            // check that the events have a schema
            if (eventList.Any(e => string.IsNullOrEmpty(e.Schema)))
                throw new EventStoreException("All events must have a schema defined.");
            
            // check that the events have data
            if (eventList.Any(e => string.IsNullOrWhiteSpace(e.Data)))
                throw new EventStoreException("All events must have data defined.");
            
            // check that the events have incrementing versions that start at expectedVersion + 1
            for (int i = 0; i < eventList.Count; i++)
            {
                if (eventList[i].Version != expectedVersion + 1 + i)
                    throw new EventStoreException($"Event at index {i} has an invalid version {eventList[i].Version}. Expected version is {expectedVersion + 1 + i}.");
            }
            
            var eventEntities = eventList.Select(e => new EventEntity
            {
                Id = e.Id,
                Created = e.Created,
                StreamId = e.StreamId,
                Version = e.Version,
                Schema = e.Schema,
                Data = e.Data,
            });

            await _eventStoreDbContext.Events.AddRangeAsync(eventEntities, cancellationToken);
            await _eventStoreDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (EventStoreException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new EventStoreException($"Failed to append events to stream with id '{streamId}'.", ex);
        }
    }
}