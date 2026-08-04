namespace EventSourcing.Mappers;

public interface IStateSerializer<TAggregate> where TAggregate : IAggregate
{
    string Type { get; }
    ISerializedState Serialize(TAggregate state);
}