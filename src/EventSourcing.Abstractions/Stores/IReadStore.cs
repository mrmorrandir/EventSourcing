using System.Collections.Immutable;
using EventSourcing.Contexts;
using FluentResults;

namespace EventSourcing.Stores;

public interface IReadStore
{
    Task<Result<List<StateEntity>>> GetStatesAsync(Guid? aggregateId = null, CancellationToken cancellationToken = default);
}