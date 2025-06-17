using FluentResults;

namespace EventSourcing.Repositories;

public interface IRepository<TAggregate> where TAggregate : IAggregate
{
    Task<Result<TAggregate>> CreateAsync<TEvent>(Func<Task<TEvent>> create, CancellationToken cancellationToken = default) where TEvent : IEvent;
    Task<Result<TAggregate>> CreateAsync<TEvent>(Func<TEvent> create, CancellationToken cancellationToken = default) where TEvent : IEvent;
    Task<Result<TAggregate>> UpdateAsync(Guid aggregateId, Func<TAggregate, Task<List<IEvent>>> update, CancellationToken cancellationToken = default);
    Task<Result<TAggregate>> UpdateAsync(Guid aggregateId, Func<TAggregate, List<IEvent>> update, CancellationToken cancellationToken = default);
    Task<Result<TAggregate>> UpdateAsync<TEvent>(Guid aggregateId, Func<TAggregate, Task<TEvent>> update, CancellationToken cancellationToken = default) where TEvent : IEvent;
    Task<Result<TAggregate>> UpdateAsync<TEvent>(Guid aggregateId, Func<TAggregate, TEvent> update, CancellationToken cancellationToken = default) where TEvent : IEvent;
}