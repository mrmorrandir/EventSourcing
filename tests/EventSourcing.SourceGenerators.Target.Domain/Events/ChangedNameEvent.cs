namespace EventSourcing.SourceGenerators.Target.Domain.Events;

public record ChangedNameEvent(Guid Id, string Name, DateTimeOffset Timestamp) : IEvent;