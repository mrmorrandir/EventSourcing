using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.Events;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

/// <summary>
/// This better be source-generated
/// </summary>
public class MyTestAggregateAggregator : IAggregator<MyTestAggregate>
{
    public MyTestAggregate CreateFromEvent(IEvent evt)
    {
        return evt switch
        {
            CreatedEvent e => MyTestAggregate.Create(e),
            _ => throw new InvalidOperationException($"Unknown event type: {evt.GetType().Name}")
        };
    }

    public MyTestAggregate ApplyEvent(MyTestAggregate aggregate, IEvent evt)
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