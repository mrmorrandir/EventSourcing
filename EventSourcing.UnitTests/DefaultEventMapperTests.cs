using EventSourcing.UnitTests.Events;
using EventSourcing.UnitTests.Mappers;

namespace EventSourcing.UnitTests;

public class DefaultEventMapperTests
{
    [Fact]
    public void Serialize_ShouldSucceed_WhenEventIsCorrect()
    {
        var mapper = new SomeDefaultEventMapper();
        var someEvent = new SomeEvent(Guid.NewGuid(), "Some text");

        var serialized = mapper.Serialize(someEvent);

        serialized.Type.Should().Be("some-event-v1");
        serialized.Data.Should().Be("{\"id\":\"" + someEvent.Id + "\",\"text\":\"" + someEvent.Text + "\"}");
    }
    
    [Fact]
    public void Deserialize_ShouldSucceed_WhenEventIsCorrect()
    {
        var mapper = new SomeDefaultEventMapper();
        var someEvent = new SomeEvent(Guid.NewGuid(), "Some text");

        var deserialized = mapper.Deserialize("some-event-v1", "{\"id\":\"" + someEvent.Id + "\",\"text\":\"" + someEvent.Text + "\"}");

        deserialized.Id.Should().Be(someEvent.Id);
        deserialized.Text.Should().Be(someEvent.Text);
    }
}