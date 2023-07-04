using EventSourcing.Abstractions;

namespace EventSourcing.FunctionTests.Mappers.Events;

public record MappingEvent(string Message): IEvent;