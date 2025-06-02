using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace EventSourcing.Stores;

/// <summary>
/// This is a very simple event store implementation. It uses a dictionary to store the events.
/// </summary>
public class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<Guid, ConcurrentBag<IEventEntity>> _events = new();

    public Task<IEnumerable<IEventEntity>> GetAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        if (!_events.TryGetValue(streamId, out var eventHistory))
            throw new EventStoreException($"Stream {streamId} not found");
        return Task.FromResult<IEnumerable<IEventEntity>>(eventHistory.OrderBy(e => e.Version).ToArray());
    }

    public Task AppendAsync(Guid streamId, int expectedVersion, IEnumerable<IEventEntity> @events, CancellationToken cancellationToken = default)
    {
        var eventStream = _events.GetOrAdd(streamId, _ => new ConcurrentBag<IEventEntity>());
        if (eventStream.Count != expectedVersion)
            throw new EventStoreException($"Stream with id {streamId} has been modified - expected version {expectedVersion} but found higher version");
        
        foreach (var @event in @events)
            eventStream.Add(@event);
        
        return Task.CompletedTask;
    }
}