using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using EventSourcing.SourceGenerators.Benchmarks;

var config = ManualConfig
    .CreateMinimumViable()
    .WithOptions(ConfigOptions.DisableOptimizationsValidator);

BenchmarkRunner.Run<EventRegistryBenchmarks>(config);