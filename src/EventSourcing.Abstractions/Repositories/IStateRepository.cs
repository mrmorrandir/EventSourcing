using FluentResults;

namespace EventSourcing.Repositories;

public interface IStateRepository<TAggregate> where TAggregate : IAggregate
{
    Task<Result<List<TAggregate>>> GetStatesAsync(Guid? aggregateId = null, long? offset = null, long? limit = null, CancellationToken cancellationToken = default);
}