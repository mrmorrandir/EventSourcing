namespace EventSourcing.Benchmarks.SerializationRegistry.Reflection;

public record TestAggregate2(Guid Id, string Name) : IAggregate
{
    public static TestAggregate2 Create(CreatedEvent2 @event) => new TestAggregate2(@event.AggregateId, @event.Name);
    public TestAggregate2 Apply(NameChangedEvent2 @event) => this with { Name = @event.Name };
}

public record CreatedEvent2(Guid AggregateId, string Name) : IEvent;
public record NameChangedEvent2(Guid AggregateId, string Name) : IEvent;