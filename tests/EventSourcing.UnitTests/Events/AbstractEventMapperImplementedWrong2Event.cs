namespace EventSourcing.UnitTests.Events;

public record AbstractEventMapperImplementedWrong2Event(Guid AggregateId, string Text) : IEvent;