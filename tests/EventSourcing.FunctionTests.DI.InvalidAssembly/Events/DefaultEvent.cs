namespace EventSourcing.FunctionTests.DI.InvalidAssembly.Events;

public record DefaultEvent(Guid Id, string Text) : IEvent;