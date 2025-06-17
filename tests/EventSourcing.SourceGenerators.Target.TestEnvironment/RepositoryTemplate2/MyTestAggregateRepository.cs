using EventSourcing.Mappers;
using EventSourcing.Projections;
using EventSourcing.Repositories;
using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.Stores;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

/// <summary>
/// This better be source-generated
/// </summary>
public class MyTestAggregateRepository : Repository<MyTestAggregate>
{
    public MyTestAggregateRepository(IEventStore eventStore, IStateStore stateStore, ISerializationRegistry<MyTestAggregate> serializationRegistry, IAggregator<MyTestAggregate> aggregator, IEnumerable<IProjector<MyTestAggregate>> projectors) : base(eventStore, stateStore, serializationRegistry, aggregator, projectors)
    {
    }
}