using EventSourcing.Mappers;
using EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment;

public partial class ChangedNameEventMapper : AbstractEventMapper<ChangedNameEvent>
{
    public ChangedNameEventMapper()
    {
        WillSerialize("changed-name-event-v1");
        CanDeserialize("changed-name-event-v1");
        Configure();
    }

    partial void Configure();
}