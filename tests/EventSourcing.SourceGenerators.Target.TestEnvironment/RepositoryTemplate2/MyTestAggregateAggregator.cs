using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

/// <summary>
/// This better be source-generated
/// </summary>
public class MyTestAggregateAggregator : IAggregator<MyTestAggregate>
{
    public Result<MyTestAggregate> CreateFromEvent(IEvent evt)
    {
        return evt switch
        {
            CreatedEvent e => Result.Try(() => MyTestAggregate.Create(e)),
            _ => new Error($"Unknown event type: {evt.GetType().Name}")
        };
    }

    public Result<MyTestAggregate> ApplyEvent(MyTestAggregate aggregate, IEvent evt)
    {
        return evt switch
        {
            ChangedNameEvent e => Result.Try(() => aggregate.Apply(e)),
            ChangedDescriptionEvent e => Result.Try(() => aggregate.Apply(e)),
            DeletedEvent e => Result.Try(() => aggregate.Apply(e)),
            _ => new Error($"Unknown event type: {evt.GetType().Name}")
        };
    }
}