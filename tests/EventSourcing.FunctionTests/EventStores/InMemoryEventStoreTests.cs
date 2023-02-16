using EventSourcing.Abstractions.Stores;
using EventSourcing.Stores;

namespace DPS2.Processes.Infrastructure.UnitTests.EventStores;

public class InMemoryEventStoreTests : EventStoreTests
{
    public override IEventStore EventStore => new InMemoryEventStore();
}