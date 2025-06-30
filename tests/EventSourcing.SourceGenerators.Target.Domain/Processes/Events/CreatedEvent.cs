namespace EventSourcing.SourceGenerators.Target.Domain.Processes.Events;

public record CreatedEvent<T>(Guid AggregateId, string Name, string Description, T Data) : IEvent;
public record StartedEvent(Guid AggregateId, DateTimeOffset Timestamp) : IEvent;
public record CompletedEvent(Guid AggregateId, ProcessResult Result, DateTimeOffset Timestamp) : IEvent;
public record CancelledEvent(Guid AggregateId, DateTimeOffset Timestamp) : IEvent;