namespace EventSourcing.FunctionTests.Mappers.Events;

public record InvalidEvent(Guid AggregateId, IntPtr Invalid, DateTime Created) : IEvent;