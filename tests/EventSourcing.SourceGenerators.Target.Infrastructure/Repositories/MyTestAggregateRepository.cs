using EventSourcing.Mappers;
using EventSourcing.Projections;
using EventSourcing.Repositories;
using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.Events;
using EventSourcing.Stores;
using Microsoft.Extensions.Logging;

namespace EventSourcing.SourceGenerators.Target.Infrastructure.Repositories;

public partial class MyTestAggregateRepository : IRepository<MyTestAggregate>
{
    
}

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

public partial class MyTestAggregateChangedDescriptionEventProjection
{
    private readonly ILogger<MyTestAggregateChangedDescriptionEventProjection> _logger;

    public MyTestAggregateChangedDescriptionEventProjection(ILogger<MyTestAggregateChangedDescriptionEventProjection> logger)
    {
        _logger = logger;
    }
    public override Task ProjectAsync(MyTestAggregate state, ChangedDescriptionEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Projecting ChangedDescriptionEvent - State: {State}", state);
        return Task.CompletedTask;
    }
}

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