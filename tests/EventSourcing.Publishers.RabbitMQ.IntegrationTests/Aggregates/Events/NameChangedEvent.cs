namespace EventSourcing.Publishers.RabbitMQ.IntegrationTests.Aggregates;

public record NameChangedEvent(Guid AggregateId, string Name) : IEvent;