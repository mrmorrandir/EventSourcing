using EventSourcing.Mappers;
using EventSourcing.UnitTests.Events;

namespace EventSourcing.UnitTests.Mappers;

public class AbstractEventMapperImplementedWrong1 : AbstractEventMapper<AbstractEventMapperImplementedWrong1Event>
{
    public AbstractEventMapperImplementedWrong1()
    {
        // No Serializer
        
        // One Deserializer
        CanDeserialize("abstract-event-mapper-implemented-wrong1-event-v1");
    }
}