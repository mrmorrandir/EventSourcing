namespace EventSourcing.FunctionTests.Mappers.Events;

public record MagicEvent(Guid AggregateId, string Magic, DateTime Created) : IEvent;