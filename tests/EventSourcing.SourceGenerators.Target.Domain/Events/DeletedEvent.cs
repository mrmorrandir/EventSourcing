namespace EventSourcing.SourceGenerators.Target.Domain.Events;

public record DeletedEvent(Guid Id, DateTimeOffset Timestamp) : IEvent;