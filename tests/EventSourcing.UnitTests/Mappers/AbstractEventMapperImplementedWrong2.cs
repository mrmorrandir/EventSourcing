using EventSourcing.Mappers;
using EventSourcing.UnitTests.Events;

namespace EventSourcing.UnitTests.Mappers;

public class AbstractEventMapperImplementedWrong2 : AbstractEventMapper<AbstractEventMapperImplementedWrong2Event>
{
    public AbstractEventMapperImplementedWrong2()
    {
        // One Serializer
        WillSerialize("abstract-event-mapper-implemented-wrong2-event-v1");
        
        // No Deserializer
    }
}