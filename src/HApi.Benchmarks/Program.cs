using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HApi.Benchmarks;

var config = ManualConfig.CreateEmpty()
    .AddColumnProvider(DefaultColumnProviders.Instance)
    .AddExporter(MarkdownExporter.GitHub)
    .AddExporter(HtmlExporter.Default)
    .AddDiagnoser(MemoryDiagnoser.Default)
    .AddJob(Job.Default
        .WithToolchain(InProcessEmitToolchain.Instance)
        .WithWarmupCount(1)
        .WithIterationCount(5));

BenchmarkRunner.Run<OrdersBenchmark>(config);
