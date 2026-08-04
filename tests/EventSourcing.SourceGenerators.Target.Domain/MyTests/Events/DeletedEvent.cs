namespace EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;

public record DeletedEvent(Guid AggregateId, DateTimeOffset Timestamp) : IEvent;