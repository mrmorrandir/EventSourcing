namespace EventSourcing.FunctionTests.DI.InvalidAssembly2.Events;

public record DefaultEvent(Guid AggregateId, string Text) : IEvent;