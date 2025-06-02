namespace EventSourcing.Benchmarks.Events;

public record MagicEvent(Guid AggregateId, string Magic, DateTime Created) : IEvent;