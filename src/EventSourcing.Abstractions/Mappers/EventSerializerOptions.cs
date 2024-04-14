using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventSourcing.Mappers;

public static class EventSerializerOptions
{
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}