using EventSourcing.Abstractions.Projections;
using EventSourcing.FunctionTests.DependencyInjections.Events;

namespace EventSourcing.FunctionTests.DependencyInjections.Projections;

public class TestProjection1 : IProjection<TestEvent1>
{
    public Task HandleAsync(TestEvent1 @event, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}