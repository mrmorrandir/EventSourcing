namespace EventSourcing.SourceGenerators.Target.Domain.Events;

public record ChangedNameEvent(Guid AggregateId, string Name, DateTimeOffset Timestamp) : IEvent;