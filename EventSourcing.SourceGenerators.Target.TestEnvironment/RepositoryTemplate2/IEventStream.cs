using System.Collections.Immutable;
using EventSourcing.Stores;
using FluentResults;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

/// <summary>
/// Don't know if I will need this.
/// </summary>
public interface IEventStream
{
    Guid StreamId { get; }
    ImmutableArray<EventEntity> Events { get; }
    int BaseVersion { get; }
    Task<Result> AppendAsync(EventEntity eventEntity, CancellationToken cancellationToken = default);
    Task<Result> SaveAsync(CancellationToken cancellationToken = default);
}