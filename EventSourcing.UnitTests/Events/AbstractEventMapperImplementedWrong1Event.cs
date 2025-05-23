namespace EventSourcing.UnitTests.Events;

public record AbstractEventMapperImplementedWrong1Event(Guid Id, string Text) : IEvent;
