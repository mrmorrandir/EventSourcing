using EventSourcing.FunctionTests.DI.ValidAssembly.Events;
using EventSourcing.Projections;

namespace EventSourcing.FunctionTests.DI.ValidAssembly.Projections;

public class CustomEventProjection : IProjection<CustomEvent>
{
    public Task HandleAsync(CustomEvent @event, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}