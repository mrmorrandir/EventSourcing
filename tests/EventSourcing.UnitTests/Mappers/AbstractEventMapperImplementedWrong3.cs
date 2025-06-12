using EventSourcing.Mappers;
using EventSourcing.UnitTests.Events;

namespace EventSourcing.UnitTests.Mappers;

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