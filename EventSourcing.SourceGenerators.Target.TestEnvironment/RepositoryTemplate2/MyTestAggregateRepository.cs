using EventSourcing.SourceGenerators.Target.Domain;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

/// <summary>
/// This better be source-generated
/// </summary>
public class MyTestAggregateRepository : Repository<MyTestAggregate>
{
    public MyTestAggregateRepository(IEventStoreX eventStore, ISerializationRegistry<MyTestAggregate> serializationRegistry, IAggregator<MyTestAggregate> aggregator) : base(eventStore, serializationRegistry, aggregator)
    {
    }
}