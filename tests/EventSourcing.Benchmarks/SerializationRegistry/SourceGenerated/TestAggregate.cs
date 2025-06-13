namespace EventSourcing.Benchmarks.SerializationRegistry;

public record TestAggregate(Guid Id, string Name) : IAggregate
{
    public static TestAggregate Create(CreatedEvent @event) => new TestAggregate(@event.AggregateId, @event.Name);
    public TestAggregate Apply(NameChangedEvent @event) => this with { Name = @event.Name };
}

public record CreatedEvent(Guid AggregateId, string Name) : IEvent;
public record NameChangedEvent(Guid AggregateId, string Name) : IEvent;