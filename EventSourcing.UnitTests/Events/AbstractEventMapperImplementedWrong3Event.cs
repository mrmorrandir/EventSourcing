namespace EventSourcing.UnitTests.Events;

public record AbstractEventMapperImplementedWrong3Event(Guid AggregateId, string Text) : IEvent;