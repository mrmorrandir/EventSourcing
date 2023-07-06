using System.Text.Json;
using System.Text.Json.Serialization;
using EventSourcing.Abstractions.Mappers;
using EventSourcing.FunctionTests.Mappers.Events;
using EventSourcing.Mappers;

namespace EventSourcing.FunctionTests.Mappers;

public class EventMapperTests
{
    [Fact]
    public void Map_ShouldNotThrowException_WhenMapIsRegistered()
    {
        var mapper = new EventMapper();
        mapper.Register<MappingEvent>("mapping-event");

        var func = () => mapper.Map(new MappingEvent("Hello World"));

        func.Should().NotThrow();
    }
    
    [Fact]
    public void Map_ShouldThrowException_WhenMapIsNotRegistered()
    {
        var mapper = new EventMapper();

        var func = () => mapper.Map(new MappingEvent("Hello World"));

        func.Should().Throw<InvalidOperationException>();
    }
}

public class CustomMap : IEventMap<MappingEventEx>
{
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
    
    public MappingEventEx Map(string type, string data)
    {
        switch (type)
        {
            case "mapping-event-ex":
                return JsonSerializer.Deserialize<MappingEventEx>(data, options)!;
            case "mapping-event":
                var mappingEvent = JsonSerializer.Deserialize<MappingEvent>(data);
                return new MappingEventEx(mappingEvent.Message, DateTime.MinValue);
            default:
                throw new InvalidOperationException($"The event type '{type}' is not supported.");
        }
    }

    public string Map(MappingEventEx @event)
    {
        return JsonSerializer.Serialize(@event, options);
    }
}