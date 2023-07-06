using EventSourcing.FunctionTests.Mappers.Events;
using EventSourcing.Mappers;

namespace EventSourcing.FunctionTests.Mappers;

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