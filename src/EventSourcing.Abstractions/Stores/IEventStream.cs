using System.Collections.Immutable;
using FluentResults;

namespace EventSourcing.Stores;

public interface IEventStream
{
    Guid StreamId { get; }
    ImmutableArray<EventEntity> Events { get; }
    int BaseVersion { get; }
    Task<Result> AppendAsync(EventEntity eventEntity, CancellationToken cancellationToken = default);
    Task<Result> SaveAsync(CancellationToken cancellationToken = default);
}