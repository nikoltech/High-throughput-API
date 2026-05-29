using HApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HApi.Data;

public static class DbInitializer
{
    private static readonly string[] Statuses = ["Pending", "Processing", "Completed", "Cancelled"];
    private static readonly string[] Products =
        ["Laptop", "Phone", "Tablet", "Monitor", "Keyboard", "Mouse", "Headset", "Webcam"];

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Orders.AnyAsync(ct)) return;

        var rng = new Random(42);

        var orders = Enumerable.Range(1, 50_000).Select(_ => new Order
        {
            CustomerId  = rng.Next(1, 5001),
            Status      = Statuses[rng.Next(Statuses.Length)],
            TotalAmount = Math.Round((decimal)(rng.NextDouble() * 2000 + 10), 2),
            CreatedAt   = DateTime.UtcNow.AddDays(-rng.Next(0, 365)),
            ProductName = Products[rng.Next(Products.Length)]
        });

        await db.Orders.AddRangeAsync(orders, ct);
        await db.SaveChangesAsync(ct);
    }

    public static async Task PurgeAsync(AppDbContext db, CancellationToken ct = default) =>
        await db.Orders.ExecuteDeleteAsync(ct);
}
