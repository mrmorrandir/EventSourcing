namespace EventSourcing.FunctionTests.DI.InvalidAssembly;

public record DefaultEvent(Guid Id, string Text) : IEvent;