using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

// var config = ManualConfig.CreateMinimumViable()
//     .WithOptions(ConfigOptions.DisableOptimizationsValidator);
//
// BenchmarkRunner.Run<MyBenchmarks>(config);

var benchmarks = new MyBenchmarks();

for (int i = 0; i < 1000; i++)
{
    await benchmarks.CreateAggregate();
}

Console.WriteLine("Done");
Console.ReadLine();