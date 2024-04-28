using EventSourcing.FunctionTests.DI.ValidAssembly.Events;
using EventSourcing.Projections;

namespace EventSourcing.FunctionTests.DI.ValidAssembly.Projections;

public class DefaultEventProjection : IProjection<DefaultEvent>
{
    public Task HandleAsync(DefaultEvent @event, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}