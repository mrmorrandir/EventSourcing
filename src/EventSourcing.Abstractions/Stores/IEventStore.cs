using System.Collections.Immutable;

namespace EventSourcing.Stores;


public interface IEventStore
{
    /// <summary>
    /// Get all events for a given stream (aggregate)
    /// </summary>
    /// <param name="streamId">The id of the stream (aggregate)</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An enumerable of <see cref="IEventEntity"/> with the complete history</returns>
    /// <throws><see cref="EventStoreException"/> if the stream does not exist</throws>
    Task<IEnumerable<IEventEntity>> GetAsync(Guid streamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an event to the event store.
    /// </summary>
    /// <param name="streamId">The id of the stream</param>
    /// <param name="expectedVersion">The version of the stream to be expected before saving</param>
    /// <param name="events">The <see cref="IEventEntity"/> to be stored in the database</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <throws><see cref="EventStoreException"/> if the expected version does not match (for new streams the expected version is 0)</throws>
    Task AppendAsync(Guid streamId, int expectedVersion, IEnumerable<IEventEntity> @events, CancellationToken cancellationToken = default);
}