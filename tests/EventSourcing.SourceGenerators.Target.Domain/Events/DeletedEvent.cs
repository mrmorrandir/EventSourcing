namespace EventSourcing.SourceGenerators.Target.Domain.Events;

public record DeletedEvent(Guid AggregateId, DateTimeOffset Timestamp) : IEvent;