using EventSourcing.SourceGenerators.Target.Domain;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

public class MyTestAggregateRepository : Repository<MyTestAggregate>
{
    public MyTestAggregateRepository(EventStoreX eventStore, ISerializationRegistry<MyTestAggregate> serializationRegistry, IAggregator<MyTestAggregate> aggregator) : base(eventStore, serializationRegistry, aggregator)
    {
    }
}