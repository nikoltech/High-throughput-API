using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace HApi.Services;

public interface IBackgroundTaskQueue
{
    ValueTask EnqueueAsync(Func<CancellationToken, ValueTask> task);
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken ct);
}

public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _channel =
        Channel.CreateBounded<Func<CancellationToken, ValueTask>>(new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

    public ValueTask EnqueueAsync(Func<CancellationToken, ValueTask> task) =>
        _channel.Writer.WriteAsync(task);

    public ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken ct) =>
        _channel.Reader.ReadAsync(ct);
}

public sealed class QueuedHostedService(IBackgroundTaskQueue queue, ILogger<QueuedHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var task in ReadAllAsync(stoppingToken))
        {
            try   { await task(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "Background task failed"); }
        }
    }

    private async IAsyncEnumerable<Func<CancellationToken, ValueTask>> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Func<CancellationToken, ValueTask> task;
            try   { task = await queue.DequeueAsync(ct); }
            catch (OperationCanceledException) { yield break; }
            yield return task;
        }
    }
}
