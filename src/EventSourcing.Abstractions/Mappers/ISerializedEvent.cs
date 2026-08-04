namespace EventSourcing.Mappers;

public interface ISerializedEvent
{
    string Schema { get; }
    string Data { get; }
}