using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using EventSourcing.Mappers;
using EventSourcing.SourceGenerators.Target;
using EventSourcing.SourceGenerators.Target.Events;
using EventSourcing.SourceGenerators.Target.Mappers;

namespace EventSourcing.SourceGenerators.Benchmarks;

[MemoryDiagnoser()]
[MarkdownExporterAttribute.Default]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class EventRegistryBenchmarks
{
    private readonly EventSourcing.Mappers.EventRegistry _reflectingRegistry;
    private readonly EventSourcing.Generated.EventRegistry _sourceGeneratedRegistry;
    private readonly IEnumerable<IEventMapper> _eventMappers = [new MyTestEventMapper()];
    
    public EventRegistryBenchmarks()
    {
        _reflectingRegistry = new EventRegistry([new MyTestEventMapper()]);
        _sourceGeneratedRegistry = new EventSourcing.Generated.EventRegistry();
    }
    
    [BenchmarkCategory("Creation"), Benchmark(Baseline = true)]
    public void Create_ReflectingRegistry_SingletonMappers()
    {
        _ = new EventRegistry(_eventMappers);
    }
    
    [BenchmarkCategory("Creation"), Benchmark]
    public void Create_ReflectingRegistry_TransientMappers()
    {
        _ = new EventRegistry([new MyTestEventMapper()]);
    }
    
    [BenchmarkCategory("Creation"), Benchmark]
    public void Create_SourceGeneratedRegistry()
    {
        _ = new EventSourcing.Generated.EventRegistry();
    }
    
    [BenchmarkCategory("Serialization"), Benchmark(Baseline = true)]
    public void Serialize_ReflectingRegistry()
    {
        var myTestEvent = new MyTestEvent("Test");
        _ = _reflectingRegistry.Serialize(myTestEvent);
    }
   
    [BenchmarkCategory("Serialization"), Benchmark]
    public void Serialize_SourceGeneratedRegistry()
    {
        var myTestEvent = new MyTestEvent("Test");
        _ = _sourceGeneratedRegistry.Serialize(myTestEvent);
    }
    
    [BenchmarkCategory("Deserialization"), Benchmark(Baseline = true)]
    public void Deserialize_ReflectingRegistry()
    {
        var data = new SerializedEvent
        {
            Type = "my-magic-test-event-v1",
            Data = "{\"Test\":\"Test\"}"
        };
        _ = _reflectingRegistry.Deserialize(data.Type, data.Data);
    }
    
    [BenchmarkCategory("Deserialization"), Benchmark]
    public void Deserialize_SourceGeneratedRegistry()
    {
        var data = new SerializedEvent
        {
            Type = "my-magic-test-event-v1",
            Data = "{\"Test\":\"Test\"}"
        };
        _ = _sourceGeneratedRegistry.Deserialize(data.Type, data.Data);
    }
}