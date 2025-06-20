using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories.MyTests;

public partial class MyTestAggregateDeletedEventProjection
{
    private readonly ILogger<MyTestAggregateDeletedEventProjection> _logger;

    public MyTestAggregateDeletedEventProjection(ILogger<MyTestAggregateDeletedEventProjection> logger)
    {
        _logger = logger;
    }
    public override Task<Result> ProjectAsync(MyTestAggregate state, DeletedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Projecting DeletedEvent - State: {State}", state);
        return Task.FromResult(Result.Ok());
    }
}