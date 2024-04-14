namespace EventSourcing.FunctionTests.DI.ValidAssembly;

public record DefaultEvent(Guid Id, string Text) : IEvent;