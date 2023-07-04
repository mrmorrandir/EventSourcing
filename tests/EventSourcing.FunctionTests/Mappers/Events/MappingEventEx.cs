using EventSourcing.Abstractions;

namespace EventSourcing.FunctionTests.Mappers.Events;

public record MappingEventEx(string Message, DateTime Timestamp): IEvent;