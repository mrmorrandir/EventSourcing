using EventSourcing.Abstractions;

namespace EventSourcing.SourceGenerators.Target.Aggregates;

public record CreatedEvent(Guid Id, string Value, DateTimeOffset Timestamp);

public record ChangedEvent(Guid Id, string Value, DateTimeOffset Timestamp);

public record MyTestAggregate(Guid Id, string Value, DateTimeOffset Timestamp) : IAggregate
{

    public static MyTestAggregate Create(CreatedEvent @event) => new MyTestAggregate(@event.Id, @event.Value, @event.Timestamp);
    
    public MyTestAggregate Apply(ChangedEvent @event) => this with { Value = @event.Value, Timestamp = @event.Timestamp };
}