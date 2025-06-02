namespace EventSourcing.Benchmarks.Events;

public record NonMagicEvent(Guid AggregateId, string Magic, DateTime Created) : IEvent;