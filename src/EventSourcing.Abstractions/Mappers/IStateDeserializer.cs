namespace EventSourcing.Mappers;

public interface IStateDeserializer<out TAggregate> where TAggregate : IAggregate
{
    string Type { get; }
    TAggregate Deserialize(string data);
}