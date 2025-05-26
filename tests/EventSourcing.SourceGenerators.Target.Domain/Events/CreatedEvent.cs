namespace EventSourcing.SourceGenerators.Target.Domain.Events;

public record CreatedEvent(Guid Id, string Name, string Description, DateTimeOffset Timestamp) : IEvent; // schema: created-event-v1, data: { "id": "guid", "name": "string", "description": "string", "timestamp": "date-time" }