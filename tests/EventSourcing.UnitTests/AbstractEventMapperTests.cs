using EventSourcing.UnitTests.Events;
using EventSourcing.UnitTests.Mappers;

namespace EventSourcing.UnitTests;

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