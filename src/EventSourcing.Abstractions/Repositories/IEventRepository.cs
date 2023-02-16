using FluentResults;

namespace EventSourcing.Abstractions.Repositories;

public interface IEventRepository
{
    Task<Result<TAggregate>> GetAsync<TAggregate>(Guid id, CancellationToken cancellationToken = default)  where TAggregate : IAggregateRoot;
    Task<Result> SaveAsync<TAggregate>(TAggregate aggregate, CancellationToken cancellationToken = default) where TAggregate : IAggregateRoot;
}