namespace EventSourcing.Benchmarks.Events;

public record DoubleMagicEvent(Guid AggregateId, string Magic, DateTime Created) : IEvent;