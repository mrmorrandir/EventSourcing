using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;
using Microsoft.Extensions.Logging;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories.MyTests;

public partial class MyTestAggregateCreatedEventProjection
{
    private readonly ILogger<MyTestAggregateCreatedEventProjection> _logger;

    public MyTestAggregateCreatedEventProjection(ILogger<MyTestAggregateCreatedEventProjection> logger)
    {
        _logger = logger;
    }
    public override Task ProjectAsync(MyTestAggregate state, CreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Projecting CreatedEvent - State: {State}", state);
        return Task.CompletedTask;
    }
}