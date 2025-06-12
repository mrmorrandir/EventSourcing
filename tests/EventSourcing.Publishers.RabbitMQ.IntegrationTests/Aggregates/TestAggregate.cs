namespace EventSourcing.Publishers.RabbitMQ.IntegrationTests.Aggregates;

public record TestAggregate(Guid Id, string Name, string Description) : IAggregate
{
    public static TestAggregate Create(CreatedEvent @event) => new TestAggregate(@event.AggregateId, @event.Name, @event.Description);
    public TestAggregate Apply(NameChangedEvent @event) => this with { Name = @event.Name };
    public TestAggregate Apply(DescriptionChangedEvent @event) => this with { Description = @event.Description };
}