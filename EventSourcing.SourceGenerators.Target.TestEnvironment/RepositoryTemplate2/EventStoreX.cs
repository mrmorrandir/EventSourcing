using EventSourcing.Contexts;
using EventSourcing.Stores;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories;


public class EventStoreX
{
    private readonly IEventStoreDbContext _context;

    public EventStoreX(IEventStoreDbContext context)
    {
        _context = context;
    }
    
    public async Task<Result<EventStream>> CreateAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        if (streamId == Guid.Empty)
            return new Error("The stream's Id must not be empty. #MissingId");

        var existingStream = await _context.Events.AsNoTracking()
            .AnyAsync(e => e.StreamId == streamId, cancellationToken);
        if (existingStream)
            return new Error($"Stream with id '{streamId}' already exists. #StreamAlreadyExists");

        var eventStream = new EventStream(_context, streamId, new List<EventEntity>());
        return eventStream;
    }

    public async Task<Result<EventStream>> GetAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        var eventsResult = await Result.Try(() => _context.Events.AsNoTracking()
            .Where(e => e.StreamId == streamId)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken));
        if (eventsResult is null)
            return new Error($"Failed to retrieve events for stream with id '{streamId}'. #FailedToRetrieveEvents");
        if (eventsResult.Value.Count == 0)
            return new Error($"Stream with id '{streamId}' not found. #StreamNotFound");

        return new EventStream(_context, streamId, eventsResult.Value);
    }
}