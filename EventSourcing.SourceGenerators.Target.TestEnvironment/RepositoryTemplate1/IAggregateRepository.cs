using EventSourcing.SourceGenerators.Target.TestEnvironment.Base;
using FluentResults;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate1;

public interface IAggregateRepository<T> where T : IAggregate
{
    Task<Result<Aggregate<T>>> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Result> SaveAsync(Guid id, IEnumerable<IEvent> events, int expectedVersion, CancellationToken cancellationToken);
    Task<Result<Aggregate<T>>> SaveAndUpdateAsync(Guid id, IEnumerable<IEvent> events, int expectedVersion, CancellationToken cancellationToken);
}