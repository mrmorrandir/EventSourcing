namespace EventSourcing.UnitTests.Events;

public record InvalidEvent(Guid Id, IntPtr Invalid, DateTime Created) : IEvent;