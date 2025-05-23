namespace EventSourcing.UnitTests.Events;

public record SomeEvent(Guid Id, string Text) : IEvent;