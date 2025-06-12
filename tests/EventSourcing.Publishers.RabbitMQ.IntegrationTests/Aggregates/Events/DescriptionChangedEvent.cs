namespace EventSourcing.Publishers.RabbitMQ.IntegrationTests.Aggregates;

public record DescriptionChangedEvent(Guid AggregateId, string Description) : IEvent;