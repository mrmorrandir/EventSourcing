namespace EventSourcing.UnitTests.Events;

public record AbstractEventMapperImplementedWrong1Event(Guid AggregateId, string Text) : IEvent;
