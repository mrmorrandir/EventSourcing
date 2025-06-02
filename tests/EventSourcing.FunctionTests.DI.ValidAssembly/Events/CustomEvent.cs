namespace EventSourcing.FunctionTests.DI.ValidAssembly.Events;

public record CustomEvent(Guid AggregateId, string Text) : IEvent;