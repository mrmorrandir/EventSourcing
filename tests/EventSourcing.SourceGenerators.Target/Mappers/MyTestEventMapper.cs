using EventSourcing.Mappers;
using EventSourcing.SourceGenerators.Target.Events;

namespace EventSourcing.SourceGenerators.Target.Mappers;

public class MyTestEventMapper : AbstractEventMapper<MyTestEvent>
{
    public MyTestEventMapper()
    {
        WillSerialize("my-test-event-v1");
        CanDeserialize("my-test-event-v1");
    }
}