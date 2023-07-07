using EventSourcing.Abstractions;
using EventSourcing.Mappers;

namespace EventSourcing.FunctionTests.Mappers;

public record SomeEvent(Guid Id, string Text) : IEvent;

public class SomeDefaultEventMapper : DefaultEventMapper<SomeEvent> { }