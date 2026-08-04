namespace EventSourcing.Stores;

public class StateEntity
{
    public Guid Id { get; init; }
    public string Schema { get; init; } = string.Empty;
    public string Data { get; init; } = string.Empty;
    public DateTimeOffset ChangedAt { get; init; } = DateTimeOffset.UtcNow;
    
    public StateEntity(){}
    public StateEntity(Guid id, string schema, string data, DateTimeOffset? changedAt = null)
    {
        Id = id;
        Schema = schema;
        Data = data;
        ChangedAt = changedAt ?? DateTimeOffset.UtcNow;
    }
}