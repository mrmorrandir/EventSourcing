using EventSourcing.Contexts;
using FluentResults;

namespace EventSourcing.Stores;

public interface IStateStore : IReadStore
{
    Task<Result> SaveStateAsync(StateEntity state, CancellationToken cancellationToken = default);
}