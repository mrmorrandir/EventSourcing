using System.Text.Json;

namespace EventSourcing.Mappers;

public class StateDeserializer<TAggregate> : IStateDeserializer<TAggregate> where TAggregate : IAggregate
{
    public string Type { get; } = ToKebabCase(typeof(TAggregate).Name);

    public StateDeserializer(string? aggregateName = null)
    {
        if (!string.IsNullOrWhiteSpace(aggregateName))
            Type = aggregateName;
    }
    public TAggregate Deserialize(string data)
    {
        try
        {
            var aggregate = JsonSerializer.Deserialize<TAggregate>(data, EventSourcingSerializerOptions.Default);
            if (aggregate == null)
                throw new EventSourcingSerializerException($"Deserialized aggregate of type {typeof(TAggregate).Name} is null");
            return aggregate;
        }
        catch (Exception e)
        {
            throw new EventSourcingSerializerException($"Failed to serialize aggregate of type {typeof(TAggregate).Name}", e);
        }
    }
    
    private static string ToKebabCase(string type) => string.Concat(type.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString())).ToLower();
}