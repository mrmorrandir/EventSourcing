using EventSourcing.Mappers;
using EventSourcing.SourceGenerators.Target.Events;

namespace EventSourcing.SourceGenerators.Target.Mappers;

public class MyTestEventMapper : AbstractEventMapper<MyTestEvent>
{
    public MyTestEventMapper()
    {
        WillSerialize("my-magic-test-event-v1");
        CanDeserialize("my-magic-test-event-v1");
    }
}