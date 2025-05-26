using System.Text.Json;

namespace EventSourcing.SourceGenerators.Target.Aggregates;

public record CreatedEvent(Guid Id, string Name, string Description, DateTimeOffset Timestamp) : IEvent; // schema: created-event-v1, data: { "id": "guid", "name": "string", "description": "string", "timestamp": "date-time" }

public record ChangedNameEvent(Guid Id, string Name, DateTimeOffset Timestamp) : IEvent;

public record ChangedDescriptionEvent(Guid Id, string Description, DateTimeOffset Timestamp) : IEvent;

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