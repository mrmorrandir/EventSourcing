namespace EventSourcing.FunctionTests.DI.ValidAssembly.Events;

public record DefaultEvent(Guid AggregateId, string Text) : IEvent;