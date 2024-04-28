namespace EventSourcing.FunctionTests.DI.InvalidAssembly2.Events;

public record DefaultEvent(Guid Id, string Text) : IEvent;