namespace EventSourcing.UnitTests.Events;

public record AbstractEventMapperImplementedWrong3Event(Guid Id, string Text) : IEvent;