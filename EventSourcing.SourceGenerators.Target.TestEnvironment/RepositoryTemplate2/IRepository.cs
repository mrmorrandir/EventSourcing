using EventSourcing.SourceGenerators.Target.Domain.Events;
using FluentResults;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

public interface IRepository<TAggregate> where TAggregate : IAggregate
{
    Task<Result<TAggregate>> CreateAsync(Func<Task<CreatedEvent>> create, CancellationToken cancellationToken = default);
    Task<Result<TAggregate>> CreateAsync(Func<CreatedEvent> create, CancellationToken cancellationToken = default);
    Task<Result<TAggregate>> UpdateAsync(Guid aggregateId, Func<TAggregate, Task<List<IEvent>>> update, CancellationToken cancellationToken = default);
    Task<Result<TAggregate>> UpdateAsync(Guid aggregateId, Func<TAggregate, List<IEvent>> update, CancellationToken cancellationToken = default);
}