using FluentResults;

namespace EventSourcing.Projections;

public interface IProjector<in TAggregate> where TAggregate : IAggregate
{
    /// <summary>
    /// This method projects the events to a new state or model.
    /// <para>
    /// When the event returns a Result.Fail, the events of the latest action are not saved.
    /// </para>
    /// </summary>
    /// <param name="state">The current (newest) state of the aggregate</param>
    /// <param name="event">The event that should be projected</param>
    /// <param name="cancellationToken">A token to cancel the task</param>
    /// <returns>A result that gives information about success or error</returns>
    Task<Result> ProjectAsync(TAggregate state, IEvent @event, CancellationToken cancellationToken = default);
}