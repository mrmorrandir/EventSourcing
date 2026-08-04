namespace EventSourcing.UnitTests.Events;

public record InvalidEvent(Guid AggregateId, IntPtr Invalid, DateTime Created) : IEvent;