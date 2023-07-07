using EventSourcing.Abstractions;

namespace EventSourcing.Benchmarks;

public record MagicEvent(Guid Id, string Magic, DateTime Created) : IEvent;