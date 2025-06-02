using EventSourcing.SourceGenerators.Target.Domain;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate1;

public interface IMyAggregateRepository : IAggregateRepository<MyTestAggregate>
{
}