namespace EventSourcing.Mappers;

public interface ISerializedState
{
    string Schema { get; }
    string Data { get; }
}