using BenchmarkDotNet.Attributes;

namespace HApi.Benchmarks;

[MemoryDiagnoser]
public class OrdersBenchmark
{
    private HttpClient _client = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        var baseUrl = Environment.GetEnvironmentVariable("API_URL") ?? "http://localhost:8080";

        _client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(120) // baseline loads 50k records
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        while (!cts.IsCancellationRequested)
        {
            try
            {
                var r = await _client.GetAsync("/health", cts.Token);
                if (r.IsSuccessStatusCode) break;
            }
            catch { }
            await Task.Delay(2000, cts.Token);
        }
    }

    [GlobalCleanup]
    public void Cleanup() => _client.Dispose();

    // Full table scan, no pagination, no projection — comparison baseline
    [Benchmark(Baseline = true)]
    public async Task Baseline() => await _client.GetAsync("/orders/baseline");

    [Benchmark]
    public async Task EfOptimized() => await _client.GetAsync("/orders?page=1&pageSize=20");

    [Benchmark]
    public async Task Dapper() => await _client.GetAsync("/orders/fast?page=1&pageSize=20");

    [Benchmark]
    public async Task OutputCache() => await _client.GetAsync("/orders?page=1&pageSize=20");

    [Benchmark]
    public async Task InMemoryCache() => await _client.GetAsync("/orders/stats");
}
