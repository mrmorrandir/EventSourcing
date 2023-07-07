using EventSourcing.Abstractions;

namespace EventSourcing.FunctionTests.Mappers;

public record UnknownEvent(Guid Id, string Text) : IEvent;