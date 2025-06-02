using EventSourcing.Mappers;
using EventSourcing.SourceGenerators.Target.Domain.Events;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories;

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