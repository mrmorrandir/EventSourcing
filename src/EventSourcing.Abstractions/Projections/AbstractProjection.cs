using FluentResults;

namespace EventSourcing.Projections;


/// <summary>
///    Abstract base class for a projection that projects an event of type <typeparamref name="TEvent" /> (coupled with an aggregate of
///     type <typeparamref name="TAggregate" />) into a user-defined state/model.
/// </summary>
public abstract class AbstractProjection<TAggregate, TEvent> : IProjection<TAggregate, TEvent>
    where TAggregate : IAggregate
    where TEvent : IEvent
{
    /// <summary>
    ///     Projects the specified <paramref name="event" /> into a user-defined state/model.
    /// <para>
    /// Together with the <paramref name="event"/>, the <paramref name="state"/> (so the current state including the applied event of the aggregate) is provided to allow the projections based on both the event and the current state of the aggregate.
    /// </para>
    /// </summary>
    /// <param name="state">The current state of the aggregate, including the applied event.</param>
    /// <param name="event">The event to project.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>Returns a result indicating the success or failure of the projection operation.</returns>
    public virtual Task<Result> ProjectAsync(TAggregate state, TEvent @event, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Fail($"Projection for event '{@event.GetType().Name}' of '{state.GetType().Namespace}' is not implemented."));
    }
}