namespace EventSourcing.Stores;

public class EventEntity : IEventEntity
{
    /// <summary>
    ///     A unique identifier for the event.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    ///     The timestamp of the event.
    /// </summary>
    public DateTimeOffset Created { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///     The StreamId of the event - this is the Id of the AggregateRoot
    /// </summary>
    public Guid StreamId { get; init; } = Guid.Empty;

    /// <summary>
    ///     The version of the event - this is the version of the AggregateRoot
    /// </summary>
    public int Version { get; init; } = 0;

    /// <summary>
    ///     The Type of the Event
    /// </summary>
    public string Schema { get; init; } = string.Empty;

    /// <summary>
    ///     The serialized data of the Event
    /// </summary>
    public string Data { get; init; } = string.Empty;
    
    public EventEntity() {}

    public EventEntity(Guid streamId, int version, string schema, string data)
    {
        StreamId = streamId;
        Version = version;
        Schema = schema;
        Data = data;
    }
}