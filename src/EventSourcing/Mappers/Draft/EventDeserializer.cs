using System.Text.Json;
using EventSourcing.Abstractions;

namespace EventSourcing.Mappers;

public class EventDeserializer<TEvent> : IEventDeserializer<TEvent> where TEvent : IEvent
{
    private readonly Func<string,JsonSerializerOptions,TEvent>? _deserializerFunc;

    public EventDeserializer(Func<string, JsonSerializerOptions, TEvent>? deserializerFunc = null)
    {
        _deserializerFunc = deserializerFunc;
    }
    public TEvent Deserialize(string data)
    {
        try
        {
            if (_deserializerFunc is not null) 
                return _deserializerFunc(data, EventSerializerOptions.Default);
            return JsonSerializer.Deserialize<TEvent>(data, EventSerializerOptions.Default)!;
        }
        catch (Exception e)
        {
            throw new EventDeserializerException($"Failed to deserialize event of type {typeof(TEvent).Name} from data: {data}", e);
        }
    }
}