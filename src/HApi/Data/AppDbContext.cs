using HApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ProductName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");

            // Partial index: only completed orders per customer
            entity.HasIndex(e => e.CustomerId)
                .HasFilter("\"Status\" = 'Completed'")
                .HasDatabaseName("idx_completed_orders");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("idx_orders_created_at");
        });
    }
}
