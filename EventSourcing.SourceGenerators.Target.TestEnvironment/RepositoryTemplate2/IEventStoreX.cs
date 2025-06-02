using FluentResults;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2;

public interface IEventStoreX
{
    Task<Result<IEventStream>> CreateStreamAsync(Guid streamId, CancellationToken cancellationToken = default);
    Task<Result<IEventStream>> GetStreamAsync(Guid streamId, CancellationToken cancellationToken = default);
}