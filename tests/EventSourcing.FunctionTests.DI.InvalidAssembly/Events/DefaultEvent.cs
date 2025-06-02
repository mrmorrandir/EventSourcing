namespace EventSourcing.FunctionTests.DI.InvalidAssembly.Events;

public record DefaultEvent(Guid AggregateId, string Text) : IEvent;