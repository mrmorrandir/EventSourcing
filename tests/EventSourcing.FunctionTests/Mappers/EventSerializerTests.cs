using System.Text.Json;
using EventSourcing.Mappers;

namespace EventSourcing.FunctionTests.Mappers;

public class EventSerializerTests
{
    [Fact]
    public void Serialize_ShouldSucceed_WhenEventIsCorrect()
    {
        var serializer = new EventSerializer<MagicEvent>("magic-event-v3");
        var magicEvent = new MagicEvent(Guid.NewGuid(), "Magic", DateTime.UtcNow);
        
        var serialized = serializer.Serialize(magicEvent);
        
        serialized.Type.Should().Be("magic-event-v3");
        serialized.Data.Should().Be("{\"id\":\"" + magicEvent.Id + "\",\"magic\":\"" + magicEvent.Magic + "\",\"created\":" + JsonSerializer.Serialize(magicEvent.Created, EventSerializerOptions.Default) + "}");
    }
    
    [Fact]
    public void Serialize_ShouldThrowException_WhenEventIsIncorrect()
    {
        var serializer = new EventSerializer<InvalidEvent>("magic-event-v3");
        var invalidEvent = new InvalidEvent(Guid.NewGuid(), IntPtr.Zero, DateTime.UtcNow);
        
        var func = () => serializer.Serialize(invalidEvent);
        
        func.Should().Throw<EventSerializerException>().WithMessage("*Failed*");
    }
}