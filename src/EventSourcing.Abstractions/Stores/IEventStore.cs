using FluentResults;

namespace EventSourcing.Stores;

public interface IEventStore
{
    Task<Result<IEventStream>> CreateStreamAsync(Guid streamId, CancellationToken cancellationToken = default);
    Task<Result<IEventStream>> GetStreamAsync(Guid streamId, CancellationToken cancellationToken = default);
}