using System.Text.Json;

namespace EventSourcing.Mappers;

public class StateSerializer<TAggregate> : IStateSerializer<TAggregate> where TAggregate : IAggregate
{
    public string Type { get; } = ToKebabCase(typeof(TAggregate).Name);

    public StateSerializer(string? aggregateName = null)
    {
        if (!string.IsNullOrEmpty(aggregateName))
            Type = aggregateName;
    }

    public ISerializedState Serialize(TAggregate state)
    {
        try
        {
            var data = JsonSerializer.Serialize(state, EventSourcingSerializerOptions.Default);
            return new SerializedState
            {
                Schema = Type,
                Data = data
            };
        }
        catch (Exception e)
        {
            throw new EventSourcingSerializerException($"Failed to serialize aggregate of type {typeof(TAggregate).Name}", e);
        }
    }
    
    private static string ToKebabCase(string type) => string.Concat(type.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString())).ToLower();
}