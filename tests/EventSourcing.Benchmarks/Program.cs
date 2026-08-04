using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using EventSourcing.Benchmarks;
using EventSourcing.Benchmarks.SerializationRegistry;

var config = ManualConfig.CreateMinimumViable()
    .WithOptions(ConfigOptions.DisableOptimizationsValidator);

BenchmarkRunner.Run<SerializationRegistryBenchmarks>(config);