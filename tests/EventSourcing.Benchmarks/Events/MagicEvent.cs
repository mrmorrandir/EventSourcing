namespace EventSourcing.Benchmarks.Events;

public record MagicEvent(Guid Id, string Magic, DateTime Created) : IEvent;