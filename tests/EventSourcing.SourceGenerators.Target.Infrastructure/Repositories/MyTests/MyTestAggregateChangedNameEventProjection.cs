using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.MyTests.Events;
using Microsoft.Extensions.Logging;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories.MyTests;

public partial class MyTestAggregateChangedNameEventProjection
{
    private readonly ILogger<MyTestAggregateChangedNameEventProjection> _logger;

    public MyTestAggregateChangedNameEventProjection(ILogger<MyTestAggregateChangedNameEventProjection> logger)
    {
        _logger = logger;
    }
    public override Task ProjectAsync(MyTestAggregate state, ChangedNameEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Projecting ChangedNameEvent - State: {State}", state);
        return Task.CompletedTask;
    }
}