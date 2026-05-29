using Dapper;
using HApi.Data;
using HApi.Models;
using HApi.Services;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

namespace HApi.Endpoints;

public static class OrdersEndpoints
{
    public static void MapOrders(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders")
            .WithTags("Orders")
            .RequireRateLimiting("default");

        group.MapGet("/baseline", GetOrdersBaseline);

        group.MapGet("/", GetOrders)
            .CacheOutput(p => p.Expire(TimeSpan.FromSeconds(30)).Tag("orders"));

        group.MapGet("/fast", GetOrdersFast);

        group.MapGet("/stats", GetStats);

        group.MapGet("/completed", GetCompleted)
            .CacheOutput(p => p.Expire(TimeSpan.FromSeconds(60)).Tag("orders-completed"));

        group.MapPost("/", CreateOrder);
    }

    // Benchmark baseline: full table scan, no pagination, no projection
    private static async Task<IResult> GetOrdersBaseline(AppDbContext db) =>
        Results.Ok(await db.Orders.ToListAsync());

    // EF: AsNoTracking + projection + enforced page size
    private static async Task<IResult> GetOrders(AppDbContext db, int page = 1, int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page     = Math.Max(page, 1);

        var query = db.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt);
        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummary(o.Id, o.CustomerId, o.TotalAmount, o.Status))
            .ToListAsync();

        return Results.Ok(new PagedResult<OrderSummary>(items, total, page, pageSize));
    }

    // Dapper: raw SQL, minimal allocations, no change tracking
    private static async Task<IResult> GetOrdersFast(NpgsqlDataSource ds, int page = 1, int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page     = Math.Max(page, 1);

        const string sql = """
            SELECT "Id", "CustomerId", "TotalAmount", "Status"
            FROM "Orders"
            ORDER BY "CreatedAt" DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        await using var conn = await ds.OpenConnectionAsync();
        var items = await conn.QueryAsync<OrderSummary>(sql, new { PageSize = pageSize, Offset = (page - 1) * pageSize });
        return Results.Ok(items);
    }

    // In-memory cache for expensive aggregation
    private static async Task<IResult> GetStats(AppDbContext db, IMemoryCache cache)
    {
        const string key = "order_stats";

        if (cache.TryGetValue(key, out var cached))
            return Results.Ok(cached);

        var stats = await db.Orders
            .AsNoTracking()
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(o => o.TotalAmount) })
            .ToListAsync();

        cache.Set(key, stats, TimeSpan.FromSeconds(600));
        return Results.Ok(stats);
    }

    // Uses partial index idx_completed_orders
    private static async Task<IResult> GetCompleted(AppDbContext db, int customerId, int page = 1, int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page     = Math.Max(page, 1);

        var items = await db.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId && o.Status == "Completed")
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummary(o.Id, o.CustomerId, o.TotalAmount, o.Status))
            .ToListAsync();

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateOrder(
        Order order,
        AppDbContext db,
        IBackgroundTaskQueue queue,
        IOutputCacheStore cacheStore,
        CancellationToken ct)
    {
        order.CreatedAt = DateTime.UtcNow;
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        // Cache invalidation runs outside the request cycle
        await queue.EnqueueAsync(async _ =>
            await cacheStore.EvictByTagAsync("orders", CancellationToken.None));

        return Results.Created($"/orders/{order.Id}", order);
    }
}
