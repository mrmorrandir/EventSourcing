namespace EventSourcing.Mappers;

public class SerializedEvent : ISerializedEvent
{
    public required string Schema { get; init; }
    public required string Data { get; init; }
}