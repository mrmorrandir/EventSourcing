using EventSourcing.Stores;

namespace EventSourcing.FunctionTests.EventStores;

public class InMemoryEventStoreTests : EventStoreTests
{
#pragma warning disable CS0618 // Type or member is obsolete
    public override IEventStore EventStore => new InMemoryEventStore();
#pragma warning restore CS0618 // Type or member is obsolete
}