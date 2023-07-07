using EventSourcing.Abstractions.Mappers;

namespace EventSourcing.Mappers;

public class SerializedEvent : ISerializedEvent
{
    public string Type { get; init; }
    public string Data { get; init; }
}