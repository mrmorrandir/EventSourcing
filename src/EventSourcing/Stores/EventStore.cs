using EventSourcing.Abstractions.Stores;
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

    public async Task<IEnumerable<IEventData>> GetAsync(Guid streamId, CancellationToken cancellationToken = default)
    {

        try
        {
            if (!await _eventStoreDbContext.Events.AnyAsync(e => e.StreamId == streamId, cancellationToken))
                throw new EventStoreException($"Stream with id {streamId} not found");
            var events = await _eventStoreDbContext.Events
                .Where(e => e.StreamId == streamId)
                .OrderBy(e => e.Version)
                .ToListAsync(cancellationToken);

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

    public async Task AppendAsync(Guid streamId, int expectedVersion, IEnumerable<IEventData> events, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await _eventStoreDbContext.Events.AnyAsync(e => e.StreamId == streamId && e.Version > expectedVersion, cancellationToken))
                throw new EventStoreException($"Stream with id {streamId} has been modified - expected version {expectedVersion} but found higher version");

            var eventEntities = events.Select(e => new EventData
            {
                Id = e.Id,
                Created = e.Created,
                StreamId = e.StreamId,
                Version = e.Version,
                Type = e.Type,
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