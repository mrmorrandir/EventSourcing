using System.Text.Json;
using EventSourcing.Abstractions;
using EventSourcing.Abstractions.Mappers;

namespace EventSourcing.Mappers;

public class EventSerializer<TEvent> : IEventSerializer<TEvent> where TEvent : IEvent
{
    public string Type { get; }
    
    public EventSerializer(string type)
    {
        Type = type;
    }
    public ISerializedEvent Serialize(TEvent @event)
    {
        try
        {
            var data = JsonSerializer.Serialize(@event, EventSerializerOptions.Default);
            return new SerializedEvent
            {
                Type = Type,
                Data = data
            };
        }
        catch (Exception e)
        {
            throw new EventSerializerException($"Failed to serialize event of type {typeof(TEvent).Name} to data", e);
        }
    }
}