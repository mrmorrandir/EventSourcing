using EventSourcing.Mappers;

namespace EventSourcing.SourceGenerators.Target;

public class MyTestEventMapper : AbstractEventMapper<MyTestEvent>
{
    public MyTestEventMapper()
    {
        WillSerialize("my-test-event-v1");
        CanDeserialize("my-test-event-v1");
    }
}