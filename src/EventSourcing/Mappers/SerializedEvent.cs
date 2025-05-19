namespace EventSourcing.Mappers;

public class SerializedEvent : ISerializedEvent
{
    public required string Type { get; init; }
    public required string Data { get; init; }
}