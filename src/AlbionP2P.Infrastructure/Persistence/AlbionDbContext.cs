using AlbionP2P.Domain.Aggregates;
using AlbionP2P.Domain.Interfaces;
using AlbionP2P.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlbionP2P.Infrastructure.Persistence;

// ── DbContext ─────────────────────────────────────────────────────────────────
public class AlbionDbContext(DbContextOptions<AlbionDbContext> options)
    : IdentityDbContext<AppUser>(options), IUnitOfWork
{
    public DbSet<Order>   Orders   => Set<Order>();
    public DbSet<Deal>    Deals    => Set<Deal>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Rating>  Ratings  => Set<Rating>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.ApplyConfiguration(new AppUserConfig());
        b.ApplyConfiguration(new OrderConfig());
        b.ApplyConfiguration(new DealConfig());
        b.ApplyConfiguration(new MessageConfig());
        b.ApplyConfiguration(new RatingConfig());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await base.SaveChangesAsync(ct);
}

// ── Configurations ────────────────────────────────────────────────────────────
file sealed class AppUserConfig : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.Property(u => u.AlbionNick).IsRequired().HasMaxLength(50);
        b.Property(u => u.ServerRegion).HasConversion<string>().HasMaxLength(20);
        b.Property(u => u.Reputation).HasDefaultValue(0);
        b.Property(u => u.CreatedAt).IsRequired();
    }
}

file sealed class OrderConfig : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.HasKey(o => o.OrderId);
        b.Property(o => o.UserId).IsRequired().HasMaxLength(450);
        b.Property(o => o.ItemName).IsRequired().HasMaxLength(200);
        b.Property(o => o.ItemCategory).HasConversion<string>().HasMaxLength(30);
        b.Property(o => o.Quantity).IsRequired();
        b.Property(o => o.Type).HasConversion<string>().HasMaxLength(10);
        b.Property(o => o.ServerRegion).HasConversion<string>().HasMaxLength(20);
        b.OwnsOne(o => o.UnitPrice, m => m.Property(x => x.Amount).HasColumnName("UnitPrice").HasPrecision(18, 2).IsRequired());
        b.OwnsOne(o => o.Status,    s => s.Property(x => x.Value).HasColumnName("Status").HasConversion<string>().HasMaxLength(20).IsRequired());
        b.Ignore(o => o.DomainEvents);
        b.HasIndex(o => o.UserId);
        b.HasIndex(o => o.CreatedAt);
    }
}

file sealed class DealConfig : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> b)
    {
        b.HasKey(d => d.DealId);
        b.Property(d => d.OrderId).IsRequired();
        b.Property(d => d.BuyerId).IsRequired().HasMaxLength(450);
        b.Property(d => d.SellerId).IsRequired().HasMaxLength(450);
        b.Property(d => d.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.OwnsOne(d => d.ProposedPrice, m => m.Property(x => x.Amount).HasColumnName("ProposedPrice").HasPrecision(18, 2).IsRequired());
        b.Ignore(d => d.DomainEvents);
        b.HasMany(d => d.Messages).WithOne().HasForeignKey(m => m.DealId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(d => d.Ratings).WithOne().HasForeignKey(r => r.DealId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(d => d.BuyerId);
        b.HasIndex(d => d.SellerId);
    }
}

file sealed class MessageConfig : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.HasKey(m => m.MessageId);
        b.Property(m => m.SenderId).IsRequired().HasMaxLength(450);
        b.Property(m => m.Content).IsRequired().HasMaxLength(1000);
        b.HasIndex(m => m.DealId);
    }
}

file sealed class RatingConfig : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> b)
    {
        b.HasKey(r => r.Id);
        b.Property(r => r.DealId).IsRequired();
        b.Property(r => r.RaterId).IsRequired().HasMaxLength(450);
        b.Property(r => r.RatedId).IsRequired().HasMaxLength(450);
        b.Property(r => r.Stars).IsRequired();
        b.Property(r => r.Comment).IsRequired().HasMaxLength(500);
        b.Property(r => r.CreatedAt).IsRequired();
        b.HasIndex(r => r.RatedId);
        b.HasIndex(r => r.RaterId);
        b.HasIndex(r => r.DealId);
    }
}
