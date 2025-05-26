namespace EventSourcing.SourceGenerators.Target.Domain.Events;

public record ChangedDescriptionEvent(Guid Id, string Description, DateTimeOffset Timestamp) : IEvent;