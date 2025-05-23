namespace EventSourcing.UnitTests.Events;

public record UnknownEvent(Guid Id, string Text) : IEvent;