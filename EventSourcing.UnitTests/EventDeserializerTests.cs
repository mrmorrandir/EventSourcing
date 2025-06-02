using System.Text.Json;
using EventSourcing.Mappers;
using EventSourcing.UnitTests.Events;

namespace EventSourcing.UnitTests;

public class EventDeserializerTests
{
    [Fact]
    public void Deserialize_ShouldSucceed_WhenEventIsCorrect()
    {
        var deserializer = new EventDeserializer<MagicEvent>();
        var magicEvent = new MagicEvent(Guid.NewGuid(), "Magic", DateTime.UtcNow);

        var deserialized = deserializer.Deserialize("{\"id\":\"" + magicEvent.AggregateId + "\",\"magic\":\"" + magicEvent.Magic + "\",\"created\":" + JsonSerializer.Serialize(magicEvent.Created, EventSerializerOptions.Default) + "}");

        deserialized.AggregateId.Should().Be(magicEvent.AggregateId);
        deserialized.Magic.Should().Be(magicEvent.Magic);
        deserialized.Created.Should().Be(magicEvent.Created);
    }
    
    [Fact]
    public void Deserialize_ShouldThrowException_WhenEventIsIncorrect()
    {
        var deserializer = new EventDeserializer<InvalidEvent>();
        var invalidEventData = "INVALID";
        
        var func = () => deserializer.Deserialize(invalidEventData);
        
        func.Should().Throw<EventDeserializerException>().WithMessage("*Failed*");
    }
}