using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using EventSourcing.Abstractions;
using EventSourcing.Abstractions.Mappers;

namespace EventSourcing.Mappers;

public class EventMap<TEvent> : IEventMap<TEvent> where TEvent : IEvent
{
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
    //private static readonly JsonSerializerOptions options = JsonSerializerOptions.Default;

    public TEvent Map(string type, string data)
    {
        try
        {
            return JsonSerializer.Deserialize<TEvent>(data, options)!;
        }
        catch (Exception e)
        {
            throw new EventMapperException($"Failed to map event of type {typeof(TEvent).Name} from data: {data}", e);
        }
    }

    public string Map(TEvent @event)
    {
        try
        {
            return JsonSerializer.Serialize(@event, options);
        }
        catch (Exception e)
        {
            throw new EventMapperException($"Failed to map event of type {typeof(TEvent).Name} to data", e);
        }
    }
}