using EventSourcing.Mappers;
using EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment;

public partial class ChangedDescriptionEventMapper : AbstractEventMapper<ChangedDescriptionEvent>
{
    public ChangedDescriptionEventMapper()
    {
        WillSerialize("changed-description-event-v1");
        CanDeserialize("changed-description-event-v1");
        Configure();
    }

    partial void Configure();
}