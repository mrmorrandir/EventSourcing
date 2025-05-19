namespace EventSourcing.FunctionTests.Mappers.Events;

public record MagicEvent(Guid Id, string Magic, DateTime Created) : IEvent;