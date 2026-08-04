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

        serialized.Schema.Should().Be("some-event-v1");
        serialized.Data.Should().Be("{\"aggregateId\":\"" + someEvent.AggregateId + "\",\"text\":\"" + someEvent.Text + "\"}");
    }
    
    [Fact]
    public void Deserialize_ShouldSucceed_WhenEventIsCorrect()
    {
        var mapper = new SomeDefaultEventMapper();
        var someEvent = new SomeEvent(Guid.NewGuid(), "Some text");

        var deserialized = mapper.Deserialize("some-event-v1", "{\"aggregateId\":\"" + someEvent.AggregateId + "\",\"text\":\"" + someEvent.Text + "\"}");

        deserialized.AggregateId.Should().Be(someEvent.AggregateId);
        deserialized.Text.Should().Be(someEvent.Text);
    }
}