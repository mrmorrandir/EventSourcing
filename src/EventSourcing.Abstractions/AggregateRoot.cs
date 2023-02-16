namespace EventSourcing.Abstractions;

public abstract class AggregateRoot : IAggregateRoot
{
    private readonly Queue<Change> _changes = new();

    public Guid Id { get; protected set; }
    public int Version { get; private set; }

    public  IEnumerable<IChange> GetChanges()
    {
        return _changes.ToArray();
    }

    public void ClearChanges()
    {
        _changes.Clear();
    }
    
    public AggregateRoot(){}

    /// <summary>
    ///     Applies multiple events to the aggregate root.
    ///     <para>Used when loading an aggregate root from the event store.</para>
    /// </summary>
    /// <param name="history">The history of the aggregate as events to by replayed</param>
    public void FromHistory(IEnumerable<IEvent> history)
    {
        foreach (var @event in history)
        {
            Apply(@event);
            Version++;
        }
    }

    /// <summary>
    ///     Applies an event to the aggregate root
    /// </summary>
    /// <param name="event"></param>
    protected abstract void Apply(IEvent @event);

    /// <summary>
    ///     Applies a change to the aggregate root and adds it to the list of changes.
    /// </summary>
    /// <param name="event"></param>
    protected void ApplyChange(IEvent @event)
    {
        Apply(@event);
        _changes.Enqueue(new Change(Version, @event));
        Version++;
    }
}