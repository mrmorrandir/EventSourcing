namespace EventSourcing.Benchmarks.Events;

public record NonMagicEvent(Guid Id, string Magic, DateTime Created) : IEvent;