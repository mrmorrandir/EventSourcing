namespace EventSourcing.FunctionTests.DI.InvalidAssembly2.Events;

public record CustomEvent(Guid Id, string Text) : IEvent;