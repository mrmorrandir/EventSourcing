using EventSourcing.Projections;
using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.Events;
using Microsoft.Extensions.Logging;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2.Projections;

/// <summary>
/// This has to be source generated (without constructor)
/// </summary>
public partial class ChangedNameEventProjection : AbstractProjection<MyTestAggregate, ChangedNameEvent>
{

}

/// <summary>
/// This has to be manually implemented
/// </summary>
public partial class ChangedNameEventProjection
{
    // This is a manually implemented projection for the ChangedNameEvent.
    // It can be used to log or perform additional actions when the name of the aggregate changes.

    private readonly ILogger<ChangedNameEventProjection> _logger;

    public ChangedNameEventProjection(ILogger<ChangedNameEventProjection> logger)
    {
        _logger = logger;
    }

    public override Task ProjectAsync(MyTestAggregate state, ChangedNameEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Projecting ChangedNameEvent for aggregate {AggregateId}", state.Id);
        return Task.CompletedTask;
    }
}