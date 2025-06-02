namespace EventSourcing.FunctionTests.Mappers.Events;

public record UnknownEvent(Guid AggregateId, string Text) : IEvent;