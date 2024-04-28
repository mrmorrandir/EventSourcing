namespace EventSourcing.FunctionTests.DI.ValidAssembly.Events;

public record CustomEvent(Guid Id, string Text) : IEvent;