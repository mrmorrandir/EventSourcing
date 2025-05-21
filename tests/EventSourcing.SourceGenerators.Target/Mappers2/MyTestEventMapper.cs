using EventSourcing.Mappers;
using EventSourcing.SourceGenerators.Target.Events;

namespace EventSourcing.SourceGenerators.Target.Mappers2;

public class MyTestEventMapper : AbstractEventMapper<MyTestEvent2>
{
    public MyTestEventMapper()
    {
        WillSerialize("my-magic-test-event-2-v1");
        CanDeserialize("my-magic-test-event-2-v1");
    }
}