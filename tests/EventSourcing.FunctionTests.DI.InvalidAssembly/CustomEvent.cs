using EventSourcing.Abstractions;

namespace EventSourcing.FunctionTests.DI.InvalidAssembly;

public record CustomEvent(Guid Id, string Text) : IEvent;