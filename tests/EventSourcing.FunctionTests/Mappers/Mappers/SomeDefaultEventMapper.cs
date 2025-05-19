using EventSourcing.Mappers;

namespace EventSourcing.FunctionTests.Mappers.Mappers;

public record SomeEvent(Guid Id, string Text) : IEvent;

public class SomeDefaultEventMapper : DefaultEventMapper<SomeEvent> { }