using EventSourcing.Contexts;
using FluentResults;

namespace EventSourcing.Stores;

public interface IStateStore
{
    Task<Result<List<StateEntity>>> GetStatesAsync(Guid? aggregateId = null, string? schema = null, long? offset = null, long? limit = 0, CancellationToken cancellationToken = default);
    Task<Result> SaveStateAsync(StateEntity state, CancellationToken cancellationToken = default);
}