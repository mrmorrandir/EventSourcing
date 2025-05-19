namespace EventSourcing.Benchmarks.Events;

public record DoubleMagicEvent(Guid Id, string Magic, DateTime Created) : IEvent;