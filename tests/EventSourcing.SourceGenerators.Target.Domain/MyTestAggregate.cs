using EventSourcing.SourceGenerators.Target.Domain.Events;

namespace EventSourcing.SourceGenerators.Target.Domain;

public record MyTestAggregate(Guid Id, string Name, string Description, bool IsDeleted, DateTimeOffset LastChanged) : IAggregate
{
    public static MyTestAggregate Create(CreatedEvent @event) => new(@event.AggregateId, @event.Name, @event.Description, false, @event.Timestamp);
    
    public MyTestAggregate Apply(ChangedNameEvent nameEvent) => this with
    {
        Name = nameEvent.Name,
        LastChanged = nameEvent.Timestamp
    };
    
    public MyTestAggregate Apply(ChangedDescriptionEvent descriptionEvent) => this with
    {
        Description = descriptionEvent.Description,
        LastChanged = descriptionEvent.Timestamp
    };
    
    public MyTestAggregate Apply(DeletedEvent deleteEvent) => this with
    {
        IsDeleted = true,
        LastChanged = deleteEvent.Timestamp
    };
}