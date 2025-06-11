using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;
using Microsoft.Extensions.Logging;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories.MyTests;

public partial class MyTestAggregateDeletedEventProjection
{
    private readonly ILogger<MyTestAggregateDeletedEventProjection> _logger;

    public MyTestAggregateDeletedEventProjection(ILogger<MyTestAggregateDeletedEventProjection> logger)
    {
        _logger = logger;
    }
    public override Task ProjectAsync(MyTestAggregate state, DeletedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Projecting DeletedEvent - State: {State}", state);
        return Task.CompletedTask;
    }
}