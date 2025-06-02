namespace EventSourcing.UnitTests.Events;

public record SomeEvent(Guid AggregateId, string Text) : IEvent;