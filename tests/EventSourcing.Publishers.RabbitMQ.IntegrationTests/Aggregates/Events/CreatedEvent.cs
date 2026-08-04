namespace EventSourcing.Publishers.RabbitMQ.IntegrationTests.Aggregates;

public record CreatedEvent(Guid AggregateId, string Name, string Description) : IEvent;