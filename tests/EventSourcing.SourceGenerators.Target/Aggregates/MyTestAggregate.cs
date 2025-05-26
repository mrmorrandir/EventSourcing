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

public class MyTestAggregateRepository
{
    private readonly Dictionary<Guid, List<object>> _streams = [];

    public MyTestAggregateRepository()
    {
        
    }
    
    public MyTestAggregate Get(Guid id)
    {
        if (!_streams.TryGetValue(id, out var events))
            throw new InvalidOperationException($"Aggregate with ID {id} not found.");

        MyTestAggregate? aggregate = null;
        foreach (var evt in events) 
            aggregate = aggregate == null ? CreateFromEvent(evt) : ApplyEvent(aggregate, evt);

        return aggregate ?? throw new InvalidOperationException($"No events found for aggregate with ID {id}.");
    }

    public void Save(Guid id, IEnumerable<IAggregateEvent> events)
    {
        if (!_streams.ContainsKey(id))
            _streams[id] = [];

        foreach (var evt in events)
            _streams[id].Add(evt);
    }

    public MyTestAggregate SaveAndGet(Guid id, IEnumerable<IAggregateEvent> events)
    {
        Save(id, events);
        return Get(id);
    }
    
    private MyTestAggregate ApplyEvent(MyTestAggregate aggregate, object evt)
    {
        return evt switch
        {
            EventSourcing.SourceGenerators.Target.Aggregates.ChangedNameEvent e => aggregate.Apply(e),
            EventSourcing.SourceGenerators.Target.Aggregates.ChangedDescriptionEvent e => aggregate.Apply(e),
            _ => throw new InvalidOperationException($"Unknown event type: {evt.GetType().Name}")
        };
    }
    private static MyTestAggregate CreateFromEvent(object evt)
    {
        return evt switch
        {
            EventSourcing.SourceGenerators.Target.Aggregates.CreatedEvent e => MyTestAggregate.Create(e),
            _ => throw new InvalidOperationException($"Unknown event type: {evt.GetType().Name}")
        };
    }
    
}