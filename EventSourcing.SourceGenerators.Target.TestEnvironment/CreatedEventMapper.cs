using EventSourcing.Mappers;
using EventSourcing.SourceGenerators.Target.Domain.Events;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment;

public partial class CreatedEventMapper : AbstractEventMapper<CreatedEvent>
{
    public CreatedEventMapper()
    {
        WillSerialize("created-event-v1");
        CanDeserialize("created-event-v1");
        Configure();
    }

    partial void Configure();
}