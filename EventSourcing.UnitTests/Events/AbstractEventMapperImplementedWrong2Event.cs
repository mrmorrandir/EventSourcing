namespace EventSourcing.UnitTests.Events;

public record AbstractEventMapperImplementedWrong2Event(Guid Id, string Text) : IEvent;