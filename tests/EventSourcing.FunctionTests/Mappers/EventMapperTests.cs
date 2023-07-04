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

public class EventMapTests
{
    [Fact]
    public void Map_ShouldReturnJson_WhenEventIsMapped()
    {
        var mapper = new EventMapper();
        mapper.Register<MappingEvent>("mapping-event");

        var json = mapper.Map(new MappingEvent("Hello World"));

        json.Should().Be("{\"message\":\"Hello World\"}");
    }

    [Fact]
    public void Map_ShouldReturnEvent_WhenJsonIsMapped()
    {
        var mapper = new EventMapper();
        mapper.Register<MappingEvent>("mapping-event");

        var @event = mapper.Map("mapping-event", "{\"message\":\"Hello World\"}");

        @event.Should().BeEquivalentTo(new MappingEvent("Hello World"));
    }

    [Fact]
    public void Map_ShouldReturnEvent_WhenJsonIsMappedWithOlderType()
    {
        var mapper = new EventMapper();
        // Altes Event (Version1)
        mapper.Register("mapping-event", new CustomMap());
        // Aktuelle Event (Verison2)
        mapper.Register("mapping-event-ex", new CustomMap());

        var @event = mapper.Map("mapping-event", "{\"message\":\"Hello World\"}");

        @event.Should().BeEquivalentTo(new MappingEventEx("Hello World", DateTime.MinValue));
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