namespace EventSourcing.FunctionTests.DI.ValidAssembly.Events;

public record DefaultEvent(Guid Id, string Text) : IEvent;