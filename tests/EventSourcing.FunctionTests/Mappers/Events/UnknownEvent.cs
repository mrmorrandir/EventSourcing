namespace EventSourcing.FunctionTests.Mappers.Events;

public record UnknownEvent(Guid Id, string Text) : IEvent;