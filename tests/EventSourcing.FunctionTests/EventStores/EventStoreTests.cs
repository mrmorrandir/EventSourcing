using EventSourcing.Abstractions.Stores;
using EventSourcing.Stores;

namespace DPS2.Processes.Infrastructure.UnitTests.EventStores;

public abstract class EventStoreTests
{
    public abstract IEventStore EventStore { get; }
    
    [Fact]
    public async Task ShouldReturnEvents_WhenStreamIdExists()
    {
        var eventStore = EventStore;
        await eventStore.AppendAsync(Guid.Parse("00000000-0000-0000-0000-000000000001"), 0, new[]
        {
            new EventData(Guid.Parse("00000000-0000-0000-0000-000000000001"), 1, "test-type", "test-data"),
            new EventData(Guid.Parse("00000000-0000-0000-0000-000000000001"), 2, "test-type", "test-data"),
            new EventData(Guid.Parse("00000000-0000-0000-0000-000000000001"), 3, "test-type", "test-data")
        });
        
        var events = (await eventStore.GetAsync(Guid.Parse("00000000-0000-0000-0000-000000000001"))).ToArray();

        events.Should().HaveCount(3);
        var eventData = events.First();
        eventData.Id.Should().NotBeEmpty();
        eventData.StreamId.Should().Be(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        eventData.Version.Should().Be(1);
        eventData.Created.Should().BeWithin(TimeSpan.FromSeconds(1));
        eventData.Type.Should().Be("test-type");
        eventData.Data.Should().Be("test-data");
        
        var eventData2 = events.Skip(1).First();
        eventData2.Id.Should().NotBeEmpty();
        eventData2.StreamId.Should().Be(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        eventData2.Version.Should().Be(2);
        eventData2.Created.Should().BeWithin(TimeSpan.FromSeconds(1));
        eventData2.Type.Should().Be("test-type");
        eventData2.Data.Should().Be("test-data");
        
        var eventData3 = events.Skip(2).First();
        eventData3.Id.Should().NotBeEmpty();
        eventData3.StreamId.Should().Be(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        eventData3.Version.Should().Be(3);
        eventData3.Created.Should().BeWithin(TimeSpan.FromSeconds(1));
        eventData3.Type.Should().Be("test-type");
        eventData3.Data.Should().Be("test-data");
    }
    
    [Fact]
    public async Task ShouldThrowException_WhenStreamIdNotExists()
    {
        var eventStore = EventStore;
        var func = () => eventStore.GetAsync(Guid.Parse("00000000-0000-0000-0000-000000000001"));

        await func.Should().ThrowAsync<EventStoreException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task ShouldThrowException_WhenStreamWasModified()
    {
        var eventStore = EventStore;
        // Original
        await eventStore.AppendAsync(Guid.Parse("00000000-0000-0000-0000-000000000002"), 0, new[]
        {
            new EventData(Guid.Parse("00000000-0000-0000-0000-000000000002"), 1, "test-type", "test-data"),
        });
        // Regular modification
        await eventStore.AppendAsync(Guid.Parse("00000000-0000-0000-0000-000000000002"), 1, new[]
        {
            new EventData(Guid.Parse("00000000-0000-0000-0000-000000000002"), 2, "test-type", "test-data"),
        });

        // "Concurrent" modification on the base of original (not allowed)
        // This happens when multiple processes are trying to modify the same stream (multiple clients currently editing the same element)
        var func = () => eventStore.AppendAsync(Guid.Parse("00000000-0000-0000-0000-000000000002"), 1, new[]
        {
            new EventData(Guid.Parse("00000000-0000-0000-0000-000000000002"), 2, "test-type", "test-data"),
        });

        await func.Should().ThrowAsync<EventStoreException>().WithMessage("*modified*");
    }
}