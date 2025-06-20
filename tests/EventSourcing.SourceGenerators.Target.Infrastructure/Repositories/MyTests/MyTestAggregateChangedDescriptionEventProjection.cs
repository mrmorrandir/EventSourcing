using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories.MyTests;

public partial class MyTestAggregateChangedDescriptionEventProjection
{
    private readonly ILogger<MyTestAggregateChangedDescriptionEventProjection> _logger;

    public MyTestAggregateChangedDescriptionEventProjection(ILogger<MyTestAggregateChangedDescriptionEventProjection> logger)
    {
        _logger = logger;
    }
    public override Task<Result> ProjectAsync(MyTestAggregate state, ChangedDescriptionEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Projecting ChangedDescriptionEvent - State: {State}", state);
        return Task.FromResult(Result.Ok());
    }
}