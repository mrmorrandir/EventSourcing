namespace EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;

public record ChangedDescriptionEvent(Guid AggregateId, string Description, DateTimeOffset Timestamp) : IEvent;