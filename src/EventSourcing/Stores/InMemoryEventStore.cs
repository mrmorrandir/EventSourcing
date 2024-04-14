using System.Collections.Concurrent;

namespace EventSourcing.Stores;

/// <summary>
/// This is a very simple event store implementation. It uses a dictionary to store the events.
/// </summary>
[Obsolete("This is a very simple event store implementation. It uses a dictionary to store the events. Use the EventStore instead.")]
public class InMemoryEventStore : IEventStore
{
    private ConcurrentDictionary<Guid, ConcurrentBag<IEventData>> _events = new();

    public Task<IEnumerable<IEventData>> GetAsync(Guid streamId, CancellationToken cancellationToken = default)
    {
        if (!_events.TryGetValue(streamId, out var eventHistory))
            throw new EventStoreException($"Stream {streamId} not found");
        return Task.FromResult<IEnumerable<IEventData>>(eventHistory.OrderBy(e => e.Version).ToArray());
    }

    public Task AppendAsync(Guid streamId, int expectedVersion, IEnumerable<IEventData> @events, CancellationToken cancellationToken = default)
    {
        var eventStream = _events.GetOrAdd(streamId, _ => new ConcurrentBag<IEventData>());
        if (eventStream.Count != expectedVersion)
            throw new EventStoreException($"Stream with id {streamId} has been modified - expected version {expectedVersion} but found higher version");
        
        foreach (var @event in @events)
            eventStream.Add(@event);
        
        return Task.CompletedTask;
    }
}