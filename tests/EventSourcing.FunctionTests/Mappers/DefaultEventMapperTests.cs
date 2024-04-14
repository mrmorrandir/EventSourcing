using EventSourcing.Mappers;

namespace EventSourcing.FunctionTests.Mappers;

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

public class AbstractEventMapperTests
{
    [Fact]
    public void Serialize_ShouldThrowException_WhenSerializerNotConfigured()
    {
        var mapper = new AbstractEventMapperImplementedWrong1();

        Action action = () => mapper.Serialize(new AbstractEventMapperImplementedWrong1Event(Guid.NewGuid(), "Some text"));

        action.Should().Throw<InvalidOperationException>();
    }
    
    [Fact]
    public void Deserialize_ShouldThrowException_WhenDeserializerNotConfigured()
    {
        var mapper = new AbstractEventMapperImplementedWrong2();

        Action action = () => mapper.Deserialize("abstract-event-mapper-implemented-wrong-event-v1", "{\"id\":\"" + Guid.NewGuid() + "\",\"text\":\"Some text\"}");

        action.Should().Throw<InvalidOperationException>();
    }
    
    [Fact]
    public void Serialize_ShouldThrowException_WhenMultipleSerializersConfigured()
    {
        var func = () => new AbstractEventMapperImplementedWrong3();
        
        func.Should().Throw<InvalidOperationException>();
    }
}

public record AbstractEventMapperImplementedWrong1Event(Guid Id, string Text) : IEvent;

public class AbstractEventMapperImplementedWrong1 : AbstractEventMapper<AbstractEventMapperImplementedWrong1Event>
{
    public AbstractEventMapperImplementedWrong1()
    {
        // No Serializer
        
        // One Deserializer
        CanDeserialize("abstract-event-mapper-implemented-wrong1-event-v1");
    }
}
public record AbstractEventMapperImplementedWrong2Event(Guid Id, string Text) : IEvent;
public class AbstractEventMapperImplementedWrong2 : AbstractEventMapper<AbstractEventMapperImplementedWrong2Event>
{
    public AbstractEventMapperImplementedWrong2()
    {
        // One Serializer
        WillSerialize("abstract-event-mapper-implemented-wrong2-event-v1");
        
        // No Deserializer
    }
}

public record AbstractEventMapperImplementedWrong3Event(Guid Id, string Text) : IEvent;
public class AbstractEventMapperImplementedWrong3 : AbstractEventMapper<AbstractEventMapperImplementedWrong3Event>
{
    public AbstractEventMapperImplementedWrong3()
    {
        // Two Serializers
        WillSerialize("abstract-event-mapper-implemented-wrong3-event-v1");
        WillSerialize("abstract-event-mapper-implemented-wrong3-event-v2");
        
        // One Deserializer
        CanDeserialize("abstract-event-mapper-implemented-wrong3-event-v1");
    }
}
