namespace EventSourcing.SourceGenerators.Target.Domain.Events;

public record ChangedDescriptionEvent(Guid AggregateId, string Description, DateTimeOffset Timestamp) : IEvent;