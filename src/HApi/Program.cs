using System.Threading.RateLimiting;
using HApi.Data;
using HApi.Endpoints;
using HApi.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(connectionString));
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

builder.Services.AddMemoryCache();
builder.Services.AddOutputCache(opt =>
    opt.AddBasePolicy(p => p.Expire(TimeSpan.FromSeconds(30))));

builder.Services.AddRateLimiter(opt =>
{
    opt.AddPolicy("default", _ =>
        RateLimitPartition.GetFixedWindowLimiter("global", _ => new FixedWindowRateLimiterOptions
        {
            Window      = TimeSpan.FromMinutes(1),
            PermitLimit = 1_000_000,
            QueueLimit  = 0
        }));
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<QueuedHostedService>();

var app = builder.Build();

app.UseOutputCache();
app.UseRateLimiter();

using (var scope = app.Services.CreateScope())
{
    var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var cfg = app.Configuration.GetSection("Database");

    db.Database.EnsureCreated();

    if (cfg.GetValue<bool>("PurgeOnStartup"))
        await DbInitializer.PurgeAsync(db);

    if (cfg.GetValue<bool>("SeedOnStartup"))
        await DbInitializer.SeedAsync(db);
}

app.MapOrders();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", utc = DateTime.UtcNow }));

app.Run();
