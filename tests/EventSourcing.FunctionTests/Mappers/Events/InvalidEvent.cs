namespace EventSourcing.FunctionTests.Mappers.Events;

public record InvalidEvent(Guid Id, IntPtr Invalid, DateTime Created) : IEvent;