using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.Events;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

public class MyTestAggregateAggregator : Aggregator<MyTestAggregate>
{
    public override MyTestAggregate CreateFromEvent(IEvent evt)
    {
        return evt switch
        {
            CreatedEvent e => MyTestAggregate.Create(e),
            _ => throw new InvalidOperationException($"Unknown event type: {evt.GetType().Name}")
        };
    }

    public override MyTestAggregate ApplyEvent(MyTestAggregate aggregate, IEvent evt)
    {
        return evt switch
        {
            ChangedNameEvent e => aggregate.Apply(e),
            ChangedDescriptionEvent e => aggregate.Apply(e),
            DeletedEvent e => aggregate.Apply(e),
            _ => throw new InvalidOperationException($"Unknown event type: {evt.GetType().Name}")
        };
    }
}