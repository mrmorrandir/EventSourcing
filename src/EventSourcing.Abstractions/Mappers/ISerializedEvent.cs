namespace EventSourcing.Abstractions.Mappers;

public interface ISerializedEvent
{
    string Type { get; }
    string Data { get; }
}