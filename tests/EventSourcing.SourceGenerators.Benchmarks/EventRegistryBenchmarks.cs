using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using EventSourcing.Generated;
using EventSourcing.Mappers;
using EventSourcing.SourceGenerators.Target.Events;

namespace EventSourcing.SourceGenerators.Benchmarks;

[MemoryDiagnoser()]
[MarkdownExporterAttribute.Default]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class EventRegistryBenchmarks
{
    private readonly EventRegistry _sourceGeneratedRegistry = new();

    [BenchmarkCategory("Creation"), Benchmark]
    public void Create_SourceGeneratedRegistry()
    {
        _ = new EventRegistry();
    }
    
    [BenchmarkCategory("Serialization"), Benchmark]
    public void Serialize_SourceGeneratedRegistry()
    {
        var myTestEvent = new MyTestEvent("Test");
        _ = _sourceGeneratedRegistry.Serialize(myTestEvent);
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