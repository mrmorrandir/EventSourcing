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
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public TEvent Map(string data)
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

    public bool TryMap(string data, out TEvent @event)
    {
        try
        {
            @event = Map(data);
            return true;
        }
        catch (Exception)
        {
            @event = default!;
            return false;
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
    
    public bool TryMap(TEvent @event, out string data)
    {
        try
        {
            data = Map(@event);
            return true;
        }
        catch (Exception)
        {
            data = default!;
            return false;
        }
    }
}