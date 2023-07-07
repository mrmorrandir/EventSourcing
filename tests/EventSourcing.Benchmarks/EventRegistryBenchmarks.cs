using System.Reflection;
using BenchmarkDotNet.Attributes;
using EventSourcing.Abstractions.Mappers;
using EventSourcing.Mappers;
using EventSourcing.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Benchmarks;

[MemoryDiagnoser()]
public class EventRegistryBenchmarks
{
    private readonly EventRegistry _registry;

    public EventRegistryBenchmarks()
    {
        var services = new ServiceCollection();
        services.AddEventMappers(config => config.AddCustomMappers(Assembly.GetExecutingAssembly()));
        var serviceProvider = services.BuildServiceProvider();
        _registry = (EventRegistry)serviceProvider.GetRequiredService<IEventRegistry>();
    }
    
    [Benchmark]
    public void Serialize()
    {
        var magicEvent = new MagicEvent(Guid.NewGuid(), "Magic", DateTime.UtcNow);
        _ = _registry.Serialize(magicEvent);
    }
   
    [Benchmark]
    public void Deserialize()
    {
        var magicEvent = new MagicEvent(Guid.NewGuid(), "Magic", DateTime.UtcNow);
        var data = _registry.Serialize(magicEvent);
        _ = _registry.Deserialize(data.Type, data.Data);
    }
    
}