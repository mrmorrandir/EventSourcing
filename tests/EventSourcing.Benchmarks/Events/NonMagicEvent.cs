namespace EventSourcing.Benchmarks;

public record NonMagicEvent(Guid Id, string Magic, DateTime Created) : IEvent;