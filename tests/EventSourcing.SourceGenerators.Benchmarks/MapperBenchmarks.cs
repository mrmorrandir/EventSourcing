using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using EventSourcing.Mappers;
using EventSourcing.SourceGenerators.Target.Aggregates;
using EventSourcing.SourceGenerators.Target.Repositories;

namespace EventSourcing.SourceGenerators.Benchmarks;

[MemoryDiagnoser()]
[MarkdownExporterAttribute.Default]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class MapperBenchmarks
{
    private readonly CreatedEventMapper _mapper = new();

    [BenchmarkCategory("Creation"), Benchmark]
    public void Create_SourceGeneratedRegistry()
    {
        _ = new CreatedEventMapper();
    }
    
    [BenchmarkCategory("Serialization"), Benchmark]
    public void Serialize_SourceGeneratedRegistry()
    {
        var myTestEvent = new CreatedEvent(Guid.NewGuid(), "Test", "Test", DateTimeOffset.Now);
        _ = _mapper.Serialize(myTestEvent);
    }
    
    [BenchmarkCategory("Deserialization"), Benchmark]
    public void Deserialize_SourceGeneratedRegistry()
    {
        var data = new SerializedEvent
        {
            Type = "created-event-v1",
            Data = "{\"Id\": \"F8DADC6C-8031-4321-B6C6-E6C13A566D67\", \"Name\": \"Test\", \"Description\": \"Test\", \"Timestamp\": \"2025-05-26T11:43:47.6176822+02:00\" }"
        };
        _ = _mapper.Deserialize(data.Type, data.Data);
    }
}