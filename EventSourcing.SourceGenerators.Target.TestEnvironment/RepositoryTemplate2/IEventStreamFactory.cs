using EventSourcing.Stores;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

/// <summary>
/// Don't know if I will need this.
/// </summary>
public interface IEventStreamFactory
{
    Task<IEventStream> CreateAsync(Guid streamId, List<EventEntity> events, CancellationToken cancellationToken = default);
}