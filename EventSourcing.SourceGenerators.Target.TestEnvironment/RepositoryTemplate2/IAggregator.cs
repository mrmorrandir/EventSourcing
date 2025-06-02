namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

public interface IAggregator<TAggregate>
{
    TAggregate CreateFromEvent(IEvent evt);
    TAggregate ApplyEvent(TAggregate aggregate, IEvent evt);
}