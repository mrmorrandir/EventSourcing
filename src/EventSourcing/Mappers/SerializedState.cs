namespace EventSourcing.Mappers;

public class SerializedState : ISerializedState
{
    public required string Schema { get; init; }
    public required string Data { get; init; }
}