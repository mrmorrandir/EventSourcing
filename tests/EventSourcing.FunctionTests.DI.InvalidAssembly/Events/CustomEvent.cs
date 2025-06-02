namespace EventSourcing.FunctionTests.DI.InvalidAssembly.Events;

public record CustomEvent(Guid AggregateId, string Text) : IEvent;