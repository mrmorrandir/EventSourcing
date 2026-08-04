using EventSourcing.Projections;
using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2.Projections;

/// <summary>
/// This has to be source generated (without constructor)
/// </summary>
public partial class CreatedEventProjection : AbstractProjection<MyTestAggregate, CreatedEvent>
{
    
}

/// <summary>
/// This has to be manually implemented
/// </summary>
public partial class CreatedEventProjection
{
    private readonly ILogger<CreatedEventProjection> _logger;

    public CreatedEventProjection(ILogger<CreatedEventProjection> logger)
    {
        _logger = logger;
    }
    
    public override Task<Result> ProjectAsync(MyTestAggregate state, CreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Projecting CreatedEvent for aggregate {AggregateId}", state.Id);
        return Task.FromResult(Result.Ok());
    }
}