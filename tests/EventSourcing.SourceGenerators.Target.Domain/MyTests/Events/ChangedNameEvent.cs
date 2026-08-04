namespace EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;

public record ChangedNameEvent(Guid AggregateId, string Name, DateTimeOffset Timestamp) : IEvent;