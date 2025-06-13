using EventSourcing.Mappers;

namespace EventSourcing.Benchmarks.SerializationRegistry.Reflection;

public class TestAggregate2NameChangedEventMapper : AbstractEventMapper<NameChangedEvent2>
{
    public TestAggregate2NameChangedEventMapper()
    {
        WillSerialize("testaggregate2-name-changed-event-v1");
        CanDeserialize("testaggregate2-name-changed-event-v1");
        CanDeserialize("testaggregate2-name-changed-event");
    }
}