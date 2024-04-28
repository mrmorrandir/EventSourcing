namespace EventSourcing.FunctionTests.Mappers;

public record MagicEvent(Guid Id, string Magic, DateTime Created) : IEvent;