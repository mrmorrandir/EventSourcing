namespace EventSourcing.FunctionTests.DI.InvalidAssembly2.Events;

public record CustomEvent(Guid AggregateId, string Text) : IEvent;