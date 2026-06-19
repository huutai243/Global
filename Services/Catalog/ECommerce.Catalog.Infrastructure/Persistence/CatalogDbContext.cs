using ECommerce.Catalog.Domain.Models;
using ECommerce.Shared.Outbox;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).HasMaxLength(200).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(2000);
            entity.Property(product => product.Price).HasPrecision(18, 2);
            entity.Property(product => product.Status).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(product => product.CategoryId);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).HasMaxLength(200).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(1000);
            entity.HasMany(category => category.Products)
                .WithOne(product => product.Category)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.MessageId)
                .IsUnique();

            builder.HasIndex(x => new { x.Status, x.NextRetryAtUtc, x.CreatedAtUtc });
            builder.HasIndex(x => new { x.Status, x.ProcessingStartedAtUtc });

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(x => x.RowVersion)
                .IsRowVersion();
        });

        base.OnModelCreating(modelBuilder);
    }
}
