using System.Reflection;
using System.Text.Json;
using EventSourcing.Mappers;
using EventSourcing.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using JsonConverter = System.Text.Json.Serialization.JsonConverter;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace EventSourcing.FunctionTests.Mappers;

public class AbstractEventMapperTests
{
    [Fact]
    public void ShouldWork()
    {
        var magicEventMapper = new MagicEventMapper();
        var magicEvent = new MagicEvent(Guid.NewGuid(), "Magic", DateTime.UtcNow);

        var data = magicEventMapper.Serialize(magicEvent);
        
        var deserializedMagicEvent = magicEventMapper.Deserialize(data.Type, data.Data);
        
        deserializedMagicEvent.Id.Should().Be(magicEvent.Id);
        deserializedMagicEvent.Magic.Should().Be(magicEvent.Magic);
        deserializedMagicEvent.Created.Should().Be(magicEvent.Created);
    }   
    
    [Fact]
    public void ShouldWork2()
    {
        var magicEventMapper = new MagicEventMapper();
        var magicEvent = new MagicEvent(Guid.NewGuid(), "Magic", DateTime.UtcNow);
        
        var data = "{ \"id\": \"" + magicEvent.Id + "\", \"magicSpell\": \"" + magicEvent.Magic + "\", \"created\": " + JsonConvert.SerializeObject(magicEvent.Created) + " }";
        
        var deserializedMagicEvent = magicEventMapper.Deserialize("magic-event-v2", data);
        
        deserializedMagicEvent.Id.Should().Be(magicEvent.Id);
        deserializedMagicEvent.Magic.Should().Be(magicEvent.Magic);
        deserializedMagicEvent.Created.Should().Be(magicEvent.Created);
    }

    [Fact]
    public void TestDi()
    {
        var services = new ServiceCollection();
        services.AddEventMappers(Assembly.GetExecutingAssembly());

        services.Should().ContainSingle(s => s.ImplementationType == typeof(MagicEventMapper));
        
        var serviceProvider = services.BuildServiceProvider();

        var mappers = serviceProvider.GetServices<IEventMapperX>();
        mappers.Should().ContainSingle(s => s.GetType() == typeof(MagicEventMapper));
    }

    [Fact]
    public void ShouldWork3()
    {
        var services = new ServiceCollection();
        services.AddEventMappers(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IEventRegistry>();
        var magicEvent = new MagicEvent(Guid.NewGuid(), "Magic", DateTime.UtcNow);
        
        var serialized = registry.Serialize(magicEvent);
        
        serialized.Type.Should().Be("magic-event-v3");
        serialized.Data.Should().Be("{\"id\":\"" + magicEvent.Id + "\",\"magic\":\"" + magicEvent.Magic + "\",\"created\":\"" + magicEvent.Created.ToString("O") + "\"}");
        
        var deserialized = (MagicEvent)registry.Deserialize(serialized.Type, serialized.Data);
        
        deserialized.Id.Should().Be(magicEvent.Id);
        deserialized.Magic.Should().Be(magicEvent.Magic);
        deserialized.Created.Should().Be(magicEvent.Created);
    }
    
    [Fact]
    public void ShouldWork4()
    {
        var services = new ServiceCollection();
        services.AddEventMappers(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IEventRegistry>();
        var magicEventV2 = new MagicEventMapper.MagicEventV2(Guid.NewGuid(), "Magic", DateTime.UtcNow);

        var serialized = JsonSerializer.Serialize(magicEventV2, EventSerializerOptions.Default);
        
        var deserialized = (MagicEvent)registry.Deserialize("magic-event-v2", serialized);
        
        deserialized.Id.Should().Be(magicEventV2.Id);
        deserialized.Magic.Should().Be(magicEventV2.MagicSpell);
        deserialized.Created.Should().Be(magicEventV2.Created);
    }
    
    [Fact]
    public void ShouldWork5()
    {
        var services = new ServiceCollection();
        services.AddEventMappers(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<IEventRegistry>();
        var magicEventV1 = new MagicEventMapper.MagicEventV1(Guid.NewGuid(), DateTime.UtcNow);

        var serialized = JsonSerializer.Serialize(magicEventV1, EventSerializerOptions.Default);
        
        var deserialized = (MagicEvent)registry.Deserialize("magic-event", serialized);
        
        deserialized.Id.Should().Be(magicEventV1.Id);
        deserialized.Magic.Should().BeEmpty();
        deserialized.Created.Should().Be(magicEventV1.Created);
    }
}