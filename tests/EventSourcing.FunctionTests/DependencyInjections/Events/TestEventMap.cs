using EventSourcing.Abstractions.Mappers;

namespace EventSourcing.FunctionTests.DependencyInjections.Events;

public class TestEventMap : IEventMap<TestEvent1>
{
    public TestEvent1 Map(string type, string data)
    {
        return new TestEvent1();
    }

    public string Map(TestEvent1 @event)
    {
        return $"The data for {@event.GetType().Name}";
    }
}