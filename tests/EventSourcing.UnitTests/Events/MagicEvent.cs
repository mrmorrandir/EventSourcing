namespace EventSourcing.UnitTests.Events;

public record MagicEvent(Guid AggregateId, string Magic, DateTime Created) : IEvent;