using EventSourcing.Mappers;
using EventSourcing.SourceGenerators.Target.Domain.Events;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories;

public partial class DeletedEventMapper : AbstractEventMapper<DeletedEvent>
{
    public DeletedEventMapper()
    {
        WillSerialize("deleted-event-v1");
        CanDeserialize("deleted-event-v1");
        Configure();
    }

    partial void Configure();
}