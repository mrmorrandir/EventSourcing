namespace EventSourcing.FunctionTests.DI.InvalidAssembly.Events;

public record CustomEvent(Guid Id, string Text) : IEvent;