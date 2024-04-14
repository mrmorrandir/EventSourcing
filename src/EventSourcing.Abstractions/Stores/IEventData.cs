namespace EventSourcing.Stores;

public interface IEventData
{
    Guid Id { get; }
    Guid StreamId { get; }
    int Version { get; }
    DateTime Created { get; }
    string Type { get; }
    string Data { get; }
}