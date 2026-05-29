```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat) (container)
Intel Core i5-8300H CPU 2.30GHz (Coffee Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.300
  [Host] : .NET 10.0.8 (10.0.826.23019), X64 RyuJIT AVX2

Toolchain=InProcessEmitToolchain  IterationCount=5  WarmupCount=1  

```
| Method        | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated  | Alloc Ratio |
|-------------- |----------:|----------:|----------:|------:|--------:|---------:|---------:|---------:|-----------:|------------:|
| Baseline      | 14.434 ms | 6.6632 ms | 1.7304 ms |  1.01 |    0.15 | 312.5000 | 312.5000 | 312.5000 | 6737.39 KB |       1.000 |
| EfOptimized   |  1.069 ms | 0.5090 ms | 0.1322 ms |  0.07 |    0.01 |   0.9766 |        - |        - |    3.88 KB |       0.001 |
| Dapper        |  1.126 ms | 0.2844 ms | 0.0739 ms |  0.08 |    0.01 |        - |        - |        - |    3.86 KB |       0.001 |
| OutputCache   |  1.156 ms | 0.3307 ms | 0.0859 ms |  0.08 |    0.01 |        - |        - |        - |    3.89 KB |       0.001 |
| InMemoryCache |  1.030 ms | 0.1901 ms | 0.0494 ms |  0.07 |    0.01 |        - |        - |        - |    2.59 KB |       0.000 |
