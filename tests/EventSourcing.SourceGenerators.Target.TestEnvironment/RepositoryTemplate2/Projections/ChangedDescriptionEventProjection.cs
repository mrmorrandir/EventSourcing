using EventSourcing.Projections;
using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2.Projections;

public class ChangedDescriptionEventProjection : AbstractProjection<MyTestAggregate, ChangedDescriptionEvent>
{
    // This is a manually implemented projection for the ChangedDescriptionEvent.
    // It can be used to log or perform additional actions when the description of the aggregate changes.

    private readonly ILogger<ChangedDescriptionEventProjection> _logger;

    public ChangedDescriptionEventProjection(ILogger<ChangedDescriptionEventProjection> logger)
    {
        _logger = logger;
    }

    public override Task<Result> ProjectAsync(MyTestAggregate state, ChangedDescriptionEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Projecting ChangedDescriptionEvent for aggregate {AggregateId}", state.Id);
        return Task.FromResult(Result.Ok());
    }
}