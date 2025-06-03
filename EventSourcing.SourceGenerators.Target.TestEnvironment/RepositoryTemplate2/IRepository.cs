using EventSourcing.SourceGenerators.Target.Domain.Events;
using FluentResults;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

public interface IRepository<TAggregate> where TAggregate : IAggregate
{
    Task<Result<TAggregate>> CreateAsync(Func<Task<IEvent>> create, CancellationToken cancellationToken = default);
    Task<Result<TAggregate>> CreateAsync(Func<IEvent> create, CancellationToken cancellationToken = default);
    Task<Result<TAggregate>> UpdateAsync(Guid aggregateId, Func<TAggregate, Task<List<IEvent>>> update, CancellationToken cancellationToken = default);
    Task<Result<TAggregate>> UpdateAsync(Guid aggregateId, Func<TAggregate, List<IEvent>> update, CancellationToken cancellationToken = default);
}