using EventSourcing.Abstractions.Projections;
using EventSourcing.FunctionTests.DependencyInjections.Events;

namespace EventSourcing.FunctionTests.DependencyInjections.Projections;

public class TestProjection2 : IProjection<TestEvent1>, IProjection<TestEvent2>
{
    public Task HandleAsync(TestEvent1 @event, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task HandleAsync(TestEvent2 @event, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}