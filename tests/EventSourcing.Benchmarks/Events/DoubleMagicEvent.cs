namespace EventSourcing.Benchmarks;

public record DoubleMagicEvent(Guid Id, string Magic, DateTime Created) : IEvent;