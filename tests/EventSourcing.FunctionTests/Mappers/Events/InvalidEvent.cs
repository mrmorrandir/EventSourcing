namespace EventSourcing.FunctionTests.Mappers;

public record InvalidEvent(Guid Id, IntPtr Invalid, DateTime Created) : IEvent;