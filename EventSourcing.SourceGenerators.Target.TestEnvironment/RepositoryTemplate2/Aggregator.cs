namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

public abstract class Aggregator<TAggregate> : IAggregator<TAggregate>
{
    public abstract TAggregate CreateFromEvent(IEvent evt);
    public abstract TAggregate ApplyEvent(TAggregate aggregate, IEvent evt);
}