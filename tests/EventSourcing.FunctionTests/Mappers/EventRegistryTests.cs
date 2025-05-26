using System.Reflection;
using EventSourcing.FunctionTests.Mappers.Events;
using EventSourcing.FunctionTests.Mappers.Mappers;
using EventSourcing.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace EventSourcing.FunctionTests.Mappers;

public class EventRegistryTests
{
    [Fact]
    public void DependencyInjection_ShouldRegisterAbstractEventMappersInAssembly()
    {
        var services = new ServiceCollection();
        services.AddEventSourcing(config =>
        {
            config.ConfigureEventStoreDbContext(options => options.UseInMemoryDatabase("Test"));
            config.ConfigureMapping(options => options.AddMappers(Assembly.GetExecutingAssembly()));
            config.ConfigureProjections(options => options.IgnoreUncoveredEvents());
        });

        services.Should().ContainSingle(x => x.ServiceType == typeof(IEventRegistry));
        
        var serviceProvider = services.BuildServiceProvider();

        var registry = serviceProvider.GetServices<IEventRegistry>();
        registry.Should().NotBeNull();
    }

    [Fact]
    public void SerializeAndDeserialize_ShouldSucceed_ForNewestVersion()
    {
        var services = new ServiceCollection();
        services.AddEventSourcing(config =>
        {
            config.ConfigureEventStoreDbContext(options => options.UseInMemoryDatabase("Test"));
            config.ConfigureMapping(options => options.AddMappers(Assembly.GetExecutingAssembly()));
            config.ConfigureProjections(options => options.IgnoreUncoveredEvents());
        });
        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IEventRegistry>();
        var magicEvent = new MagicEvent(Guid.NewGuid(), "Magic", DateTime.UtcNow);
        
        var serialized = registry.Serialize(magicEvent);
        
        serialized.Schema.Should().Be("magic-event-v3");
        serialized.Data.Should().Be("{\"id\":\"" + magicEvent.Id + "\",\"magic\":\"" + magicEvent.Magic + "\",\"created\":" + JsonSerializer.Serialize(magicEvent.Created, EventSerializerOptions.Default) + "}");
        
        var deserialized = (MagicEvent)registry.Deserialize(serialized.Schema, serialized.Data);
        
        deserialized.Id.Should().Be(magicEvent.Id);
        deserialized.Magic.Should().Be(magicEvent.Magic);
        deserialized.Created.Should().Be(magicEvent.Created);
    }
    
    [Fact]
    public void Deserialize_ShouldSucceed_ForOldVersion2()
    {
        var services = new ServiceCollection();
        services.AddEventSourcing(config =>
        {
            config.ConfigureEventStoreDbContext(options => options.UseInMemoryDatabase("Test"));
            config.ConfigureMapping(options => options.AddMappers(Assembly.GetExecutingAssembly()));
            config.ConfigureProjections(options => options.IgnoreUncoveredEvents());
        });
        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IEventRegistry>();
        var magicEventV2 = new MagicEventV2(Guid.NewGuid(), "Magic", DateTime.UtcNow);

        var serialized = JsonSerializer.Serialize(magicEventV2, EventSerializerOptions.Default);
        
        var deserialized = (MagicEvent)registry.Deserialize("magic-event-v2", serialized);
        
        deserialized.Id.Should().Be(magicEventV2.Id);
        deserialized.Magic.Should().Be(magicEventV2.MagicSpell);
        deserialized.Created.Should().Be(magicEventV2.Created);
    }
    
    [Fact]
    public void Deserialize_ShouldSucceed_ForOldVersion1()
    {
        var services = new ServiceCollection();
        services.AddEventSourcing(config =>
        {
            config.ConfigureEventStoreDbContext(options => options.UseInMemoryDatabase("Test"));
            config.ConfigureMapping(options => options.AddMappers(Assembly.GetExecutingAssembly()));
            config.ConfigureProjections(options => options.IgnoreUncoveredEvents());
        });
        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IEventRegistry>();
        var magicEventV1 = new MagicEventV1(Guid.NewGuid(), DateTime.UtcNow);

        var serialized = JsonSerializer.Serialize(magicEventV1, EventSerializerOptions.Default);
        
        var deserialized = (MagicEvent)registry.Deserialize("magic-event", serialized);
        
        deserialized.Id.Should().Be(magicEventV1.Id);
        deserialized.Magic.Should().BeEmpty();
        deserialized.Created.Should().Be(magicEventV1.Created);
    }

    [Fact]
    public void Deserialize_ShouldThrowException_WhenMapperNotFound()
    {
        var services = new ServiceCollection();
        services.AddEventSourcing(config =>
        {
            config.ConfigureEventStoreDbContext(options => options.UseInMemoryDatabase("Test"));
            config.ConfigureMapping(options => options.AddMappers(Assembly.GetExecutingAssembly()));
            config.ConfigureProjections(options => options.IgnoreUncoveredEvents());
        });
        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IEventRegistry>();
        var magicEvent = new MagicEvent(Guid.NewGuid(), "Magic", DateTime.UtcNow);
        var serialized = JsonSerializer.Serialize(magicEvent, EventSerializerOptions.Default);

        var func = () => (MagicEvent) registry.Deserialize("magic-event-vUNKOWN", serialized);

        func.Should().Throw<EventRegistryException>().WithMessage("*no deserializer found*");
    }
    
    [Fact]
    public void Serialize_ShouldThrowException_WhenMapperNotFound()
    {
        var services = new ServiceCollection();
        services.AddEventSourcing(config =>
        {
            config.ConfigureEventStoreDbContext(options => options.UseInMemoryDatabase("Test"));
            config.ConfigureMapping(options => options.AddMappers(Assembly.GetExecutingAssembly()));
            config.ConfigureProjections(options => options.IgnoreUncoveredEvents());
        });
        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IEventRegistry>();

        // This UnknownEvent is not registered in the registry because it is not in the assembly
        var func = () => registry.Serialize(new EventSourcing.UnitTests.Events.UnknownEvent(Guid.Empty, "Unknown"));

        func.Should().Throw<EventRegistryException>().WithMessage("*no serializer found*");
    }
}