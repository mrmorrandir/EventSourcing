using EventSourcing.SourceGenerators.Target.Domain;
using EventSourcing.SourceGenerators.Target.Domain.Events;
using FluentResults;

namespace EventSourcing.SourceGenerators.Target.TestEnvironment.RepositoryTemplate2.Projections;

/// <summary>
/// This has to be source generated
/// </summary>
/// <typeparam name="TAggregate"></typeparam>
public class MyTestAggregateProjector : IProjector<MyTestAggregate> 
{
    private readonly CreatedEventProjection _createdEventProjection;
    private readonly ChangedNameEventProjection _changedNameEventProjection;
    private readonly ChangedDescriptionEventProjection _changedDescriptionEventProjection;

    /// <summary>
    /// Events must be registered
    /// </summary>
    /// <param name="createdEventProjection"></param>
    /// <param name="changedNameEventProjection"></param>
    public MyTestAggregateProjector(CreatedEventProjection createdEventProjection, ChangedNameEventProjection changedNameEventProjection, ChangedDescriptionEventProjection changedDescriptionEventProjection)
    {
        _createdEventProjection = createdEventProjection;
        _changedNameEventProjection = changedNameEventProjection;
        _changedDescriptionEventProjection = changedDescriptionEventProjection;
    }
    
    public async Task<Result> ProjectAsync(MyTestAggregate state, IEvent @event, CancellationToken cancellationToken = default)
    {
        return @event.GetType() switch
        {
            {  } type when type == typeof(CreatedEvent) => await Result.Try(() => _createdEventProjection.ProjectAsync(state, (CreatedEvent)@event, cancellationToken)),
            {  } type when type == typeof(ChangedNameEvent) => await Result.Try(() => _changedNameEventProjection.ProjectAsync(state, (ChangedNameEvent)@event, cancellationToken)),
            {  } type when type == typeof(ChangedDescriptionEvent) => await Result.Try(() => _changedDescriptionEventProjection.ProjectAsync(state, (ChangedDescriptionEvent)@event, cancellationToken)),
            _ => Result.Fail($"Projection for event '{@event.GetType().Name}' of '{state.GetType().Namespace}' is not implemented.")
        };
    }
}