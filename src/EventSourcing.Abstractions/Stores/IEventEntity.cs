namespace EventSourcing.Stores;

public interface IEventEntity
{
    Guid Id { get; }
    Guid StreamId { get; }
    int Version { get; }
    DateTimeOffset Created { get; }
    string Schema { get; }
    string Data { get; }
}