namespace EventSourcing.FunctionTests.DI.ValidAssembly;

public record CustomEvent(Guid Id, string Text) : IEvent;