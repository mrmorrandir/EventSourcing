namespace EventSourcing.Stores;


public interface IEventStore
{
    /// <summary>
    /// Get all events for a given stream (aggregate)
    /// </summary>
    /// <param name="streamId">The id of the stream (aggregate)</param>
    /// <returns>An enumerable of <see cref="EventData"/> with the complete history</returns>
    /// <throws><see cref="EventStoreException"/> if the stream does not exist</throws>
    Task<IEnumerable<IEventData>> GetAsync(Guid streamId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves an event to the event store.
    /// </summary>
    /// <param name="streamId">The id of the stream</param>
    /// <param name="expectedVersion">The version of the stream to be expected before saving</param>
    /// <param name="event">The <see cref="EventData"/> to be stored in the database</param>
    /// <returns></returns>
    /// <throws><see cref="EventStoreException"/> if the expected version does not match (for new streams the expected version is 0)</throws>
    Task AppendAsync(Guid streamId, int expectedVersion, IEnumerable<IEventData> @events, CancellationToken cancellationToken = default);
}