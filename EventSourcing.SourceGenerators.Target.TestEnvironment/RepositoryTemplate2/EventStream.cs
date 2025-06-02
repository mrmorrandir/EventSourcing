using System.Collections.Immutable;
using EventSourcing.Contexts;
using EventSourcing.Stores;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories;

public class EventStream  
{
    private readonly IEventStoreDbContext _context;
    private readonly Guid _streamId;
    private readonly List<EventEntity> _events;
    private readonly List<EventEntity> _appendedEvents = [];
    private int _baseVersion;
    
    public Guid StreamId => _streamId;
    public ImmutableArray<EventEntity> Events => [.._events];
    
    public int BaseVersion => _baseVersion;

    public EventStream(IEventStoreDbContext context, Guid streamId, List<EventEntity> events)
    {
        _context = context;
        _streamId = streamId;
        _events = events;
        _baseVersion = events.Count > 0 ? events.Max(x => x.Version) : 0;
    }
    
    public Task<Result> AppendAsync(EventEntity eventEntity, CancellationToken cancellationToken = default)
    {
        if (eventEntity.Id == Guid.Empty)
            return Task.FromResult<Result>(new Error("The event's Id must not be empty. #MissingId"));
        
        if (eventEntity.StreamId != _streamId)
            return Task.FromResult<Result>(new Error($"The event's StreamId '{_streamId}' does not match the stream id '{eventEntity.StreamId}' of the EventStream. #StreamIdMismatch"));
        
        if (eventEntity.Created.Offset != TimeSpan.Zero)
            return Task.FromResult<Result>(new Error("The event's Created timestamp must be in UTC. #InvalidTimestamp"));
        
        if (string.IsNullOrEmpty(eventEntity.Schema))
            return Task.FromResult<Result>(new Error("The event's Schema must not be empty. #MissingSchema"));
        
        if (string.IsNullOrWhiteSpace(eventEntity.Data))
            return Task.FromResult<Result>(new Error("The event's Data must not be empty. #MissingData"));

        if (eventEntity.Version != (_events.Max(x => x.Version) + 1))
            return Task.FromResult<Result>(new Error($"The event's Version {eventEntity.Version} does not match the expected version {(_events.Max(x => x.Version) + 1)} for the stream with id '{_streamId}'. #VersionMismatch"));
        
        _appendedEvents.Add(eventEntity);
        return Task.FromResult(Result.Ok());
    }

    public async Task<Result> SaveAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Start a transaction here
        if (_baseVersion != 0)
        {
            var expectedVersion = await _context.Events.Where(x => x.StreamId == _streamId).MaxAsync(x => x.Version, cancellationToken);
            if (expectedVersion != _baseVersion)
                return new Error($"Stream with id '{_streamId}' has been modified - expected version {_baseVersion} but found higher version {expectedVersion}. #StreamModified");
        }

        foreach (var eventData in _appendedEvents)
        {
            var addResult = await Result.Try(() => _context.Events.AddAsync(eventData, cancellationToken));
            if (addResult.IsFailed)
                return new Error($"Failed to add event to the stream with id '{_streamId}': {addResult.Errors.First().Message}. #FailedToAddEvent");
        }

        var saveResult = await Result.Try(() => _context.SaveChangesAsync(cancellationToken));
        if (saveResult.IsFailed)
            return new Error($"Failed to save events for stream with id '{_streamId}': {saveResult.Errors.First().Message}. #FailedToSaveEvents");
        
        _events.AddRange(_appendedEvents);
        _appendedEvents.Clear();
        _baseVersion = _events.Max(x => x.Version);
        // TODO: End Transaction here
        return Result.Ok();
    }
}