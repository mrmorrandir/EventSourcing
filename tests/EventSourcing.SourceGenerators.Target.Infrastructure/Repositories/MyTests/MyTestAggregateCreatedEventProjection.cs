using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories.MyTests;

public partial class MyTestAggregateCreatedEventProjection
{
    private readonly ILogger<MyTestAggregateCreatedEventProjection> _logger;

    public MyTestAggregateCreatedEventProjection(ILogger<MyTestAggregateCreatedEventProjection> logger)
    {
        _logger = logger;
    }
    public override Task<Result> ProjectAsync(MyTestAggregate state, CreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Projecting CreatedEvent - State: {State}", state);
        return Task.FromResult(Result.Ok());
    }
}