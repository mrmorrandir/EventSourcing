using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using EventSourcing.Benchmarks.SerializationRegistry.Reflection;
using EventSourcing.Mappers;
using FluentResults;

namespace EventSourcing.Benchmarks.SerializationRegistry;

[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[MemoryDiagnoser()]
public class SerializationRegistryBenchmarks
{
    private readonly SerializationRegistry<TestAggregate2> _reflectionRegistry;
    private readonly TestAggregateSerializationRegistry _sourceGeneratedRegistry;
    
    private readonly CreatedEvent2 _createdEventReflectionRegistry = new CreatedEvent2(Guid.Empty, "Test");
    private readonly CreatedEvent _createdEventSourceGeneratedRegistry = new CreatedEvent(Guid.Empty, "Test");
    
    public SerializationRegistryBenchmarks()
    {
        _reflectionRegistry = new SerializationRegistry<TestAggregate2>();
        _sourceGeneratedRegistry = new TestAggregateSerializationRegistry();
    }
    
    [BenchmarkCategory("Create"), Benchmark(Baseline = true)]
    public SerializationRegistry<TestAggregate2> Create_ReflectionRegistry()
    {
        return new SerializationRegistry<TestAggregate2>();
    }
    
    [BenchmarkCategory("Create"), Benchmark]
    public TestAggregateSerializationRegistry Create_SourceGeneratedRegistry()
    {
        return new TestAggregateSerializationRegistry();
    }
    
    [BenchmarkCategory("Serialize"), Benchmark(Baseline = true)]
    public Result<ISerializedEvent> Serialize_ReflectionRegistry()
    {
        var result = _reflectionRegistry.Serialize(_createdEventReflectionRegistry);
        if (result.IsFailed)
            throw new InvalidOperationException($"Failed to serialize event: {result.Errors[0].Message}");
        return result;
    }
    
    [BenchmarkCategory("Serialize"), Benchmark]
    public Result<ISerializedEvent> Serialize_SourceGeneratedRegistry()
    {
        var result = _sourceGeneratedRegistry.Serialize(_createdEventSourceGeneratedRegistry);
        if (result.IsFailed)
            throw new InvalidOperationException($"Failed to serialize event: {result.Errors[0].Message}");
        return result;
    }
    
    [BenchmarkCategory("Deserialize"), Benchmark(Baseline = true)]
    public Result<IEvent> Deserialize_ReflectionRegistry()
    {
        var result = _reflectionRegistry.Deserialize("testaggregate2-created-event-v1", "{\"aggregateId\":\"00000000-0000-0000-0000-000000000000\",\"name\":\"Test\"}");
        if (result.IsFailed)
            throw new InvalidOperationException($"Failed to deserialize event: {result.Errors[0].Message}");
        return result;
    }
    
    [BenchmarkCategory("Deserialize"), Benchmark]
    public Result<IEvent> Deserialize_SourceGeneratedRegistry()
    {
        var result = _sourceGeneratedRegistry.Deserialize("testaggregate-created-event-v1", "{\"aggregateId\":\"00000000-0000-0000-0000-000000000000\",\"name\":\"Test\"}");
        if (result.IsFailed)
            throw new InvalidOperationException($"Failed to deserialize event: {result.Errors[0].Message}");
        return result;
    }
}