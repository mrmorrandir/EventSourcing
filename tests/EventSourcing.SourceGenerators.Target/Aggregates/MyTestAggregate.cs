using EventSourcing.Abstractions;
using EventSourcing.SourceGenerators.Target.Aggregates.Generated;

namespace EventSourcing.SourceGenerators.Target.Aggregates;

public record CreatedEvent(Guid Id, string Name, string Description, DateTimeOffset Timestamp) : IAggregateEvent;

public record ChangedNameEvent(Guid Id, string Name, DateTimeOffset Timestamp) : IAggregateEvent;

public record ChangedDescriptionEvent(Guid Id, string Description, DateTimeOffset Timestamp) : IAggregateEvent;

public record MyTestAggregate(Guid Id, string Name, string Description, DateTimeOffset LastChanged) : IAggregate
{

    public static MyTestAggregate Create(CreatedEvent @event) => new(@event.Id, @event.Name, @event.Description, @event.Timestamp);
    
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
}