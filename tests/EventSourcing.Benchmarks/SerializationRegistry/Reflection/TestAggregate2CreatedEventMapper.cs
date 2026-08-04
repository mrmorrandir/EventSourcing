using EventSourcing.Mappers;

namespace EventSourcing.Benchmarks.SerializationRegistry.Reflection;

public class TestAggregate2CreatedEventMapper : AbstractEventMapper<CreatedEvent2>
{
    public TestAggregate2CreatedEventMapper()
    {
        WillSerialize("testaggregate2-created-event-v1");
        CanDeserialize("testaggregate2-created-event-v1");
        CanDeserialize("testaggregate2-created-event");
    }
}