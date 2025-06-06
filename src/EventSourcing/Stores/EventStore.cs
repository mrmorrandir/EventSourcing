using EventSourcing.Contexts;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.Stores;

public class EventStore : IEventStore
{
    private readonly IEventStoreDbContext _context;

    public EventStore(IEventStoreDbContext context)
    {
        _context = context;
    }
    
    public async Task<Result<IEventStream>> CreateStreamAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        if (streamId == Guid.Empty)
            return new Error("The stream's Id must not be empty. #MissingId");

        var existingStreamResult = await Result.Try(() => _context.Events.AsNoTracking().AnyAsync(e => e.StreamId == streamId, cancellationToken));
        if (existingStreamResult.IsFailed)
            return new Error($"Failed to check if stream with id '{streamId}' exists. #FailedToCheckStreamExists");
        
        if (existingStreamResult.Value)
            return new Error($"Stream with id '{streamId}' already exists. #StreamAlreadyExists");

        var eventStream = new EventStream(_context, streamId, []);
        return eventStream;
    }

    public async Task<Result<IEventStream>> GetStreamAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        var eventsResult = await Result.Try(() => _context.Events.AsNoTracking()
            .Where(e => e.StreamId == streamId)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken));
        
        if (eventsResult.IsFailed)
            return new Error($"Failed to retrieve events for stream with id '{streamId}'. #FailedToRetrieveEvents");
        
        if (eventsResult.Value.Count == 0)
            return new Error($"Stream with id '{streamId}' not found. #StreamNotFound");

        return new EventStream(_context, streamId, eventsResult.Value);
    }
}