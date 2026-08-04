using EventSourcing.SourceGenerators.Target.Domain.Processes.Events;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories.Processes;

public partial class ProcessOfLubricantDataCompletedEventProjection
{
    public override Task<Result> ProjectAsync(
        Process<LubricantData> state,
        CompletedEvent @event,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok());
    }
}
