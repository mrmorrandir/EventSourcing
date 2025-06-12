namespace EventSourcing.UnitTests.Events;

public record UnknownEvent(Guid AggregateId, string Text) : IEvent;