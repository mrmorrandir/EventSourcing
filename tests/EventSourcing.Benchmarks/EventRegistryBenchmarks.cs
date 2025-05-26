using System.Reflection;
using BenchmarkDotNet.Attributes;
using EventSourcing.Benchmarks.Events;
using EventSourcing.Mappers;
using EventSourcing.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Benchmarks;

[MemoryDiagnoser()]
public class EventRegistryBenchmarks
{
    // private readonly EventRegistry _registry;
    //
    // public EventRegistryBenchmarks()
    // {
    //     var services = new ServiceCollection();
    //     services.AddEventSourcing(config =>
    //     {
    //         config.ConfigureEventStoreDbContext(options => options.UseInMemoryDatabase("Benchmark"));
    //         config.ConfigureMapping(options => options.AddMappers(Assembly.GetExecutingAssembly()));
    //         config.ConfigureProjections(options => options.AddProjections(Assembly.GetExecutingAssembly()).IgnoreUncoveredEvents());
    //     });
    //     var serviceProvider = services.BuildServiceProvider();
    //     _registry = (EventRegistry)serviceProvider.GetRequiredService<IEventRegistry>();
    // }
    //
    // [Benchmark]
    // public void Serialize()
    // {
    //     var magicEvent = new MagicEvent(Guid.NewGuid(), "Magic", DateTime.UtcNow);
    //     _ = _registry.Serialize(magicEvent);
    // }
    //
    // [Benchmark]
    // public void Deserialize()
    // {
    //     var magicEvent = new MagicEvent(Guid.NewGuid(), "Magic", DateTime.UtcNow);
    //     var data = _registry.Serialize(magicEvent);
    //     _ = _registry.Deserialize(data.Type, data.Data);
    // }
    //
}