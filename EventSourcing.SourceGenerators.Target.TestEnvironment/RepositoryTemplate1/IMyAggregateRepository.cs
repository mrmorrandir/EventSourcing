using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Infrastructure.Repositories;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate1;

public interface IMyAggregateRepository : IAggregateRepository<MyTestAggregate>
{
}