namespace EventSourcing;

public interface IAggregateRoot
{
    Guid Id { get; }
    int Version { get; }
    IEnumerable<IChange> GetChanges();
    void ClearChanges();

    /// <summary>
    ///     Applies multiple events to the aggregate root.
    ///     <para>Used when loading an aggregate root from the event store.</para>
    /// </summary>
    /// <param name="history">The history of the aggregate as events to by replayed</param>
    void FromHistory(IEnumerable<IEvent> history);
}