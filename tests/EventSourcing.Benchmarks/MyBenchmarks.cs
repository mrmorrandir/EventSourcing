using BenchmarkDotNet.Attributes;
using EventSourcing.Abstractions.Repositories;
using EventSourcing.Abstractions.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Benchmarks;

[MemoryDiagnoser()]
public class MyBenchmarks
{
    private readonly IEventStore _eventStore;
    private readonly IEventRepository _eventRepository;

    public MyBenchmarks()
    {
        var services = new ServiceCollection();
        services.AddEventSourcing(options => InMemoryDbContextOptionsExtensions.UseInMemoryDatabase(options, "Benchmarks"));

        var provider = services.BuildServiceProvider();
        provider.AddEventMappings(config =>
        {
            config.Register<TestCreated>("test-created");
            config.Register<TestChanged>("test-changed");
        });

        _eventStore = provider.GetRequiredService<IEventStore>();
        _eventRepository = provider.GetRequiredService<IEventRepository>();
    }
    
    [Benchmark]
    public async Task CreateAggregate()
    {
        var aggregate = TestAggregate.Create("Test");
        await _eventRepository.SaveAsync(aggregate);
        aggregate.Change("Hallo Test");
        await _eventRepository.SaveAsync(aggregate);
        aggregate.Change("Change Back");
        await _eventRepository.SaveAsync(aggregate);
    }
}