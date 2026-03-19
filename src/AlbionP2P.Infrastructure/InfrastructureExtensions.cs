using AlbionP2P.Domain.Aggregates;
using AlbionP2P.Domain.Interfaces;
using AlbionP2P.Domain.ValueObjects;
using AlbionP2P.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlbionP2P.Infrastructure;

// ── Repositories ──────────────────────────────────────────────────────────────
public sealed class OrderRepository(AlbionDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Orders.FirstOrDefaultAsync(o => o.OrderId == id, ct);

    public async Task<List<Order>> GetRecentAsync(ItemCategory? cat, OrderType? type, ServerRegion? region, int page, int size, CancellationToken ct = default)
    {
        var q = db.Orders
            .Where(o => o.Status.Value == OrderStatusValue.Open || o.Status.Value == OrderStatusValue.InNegotiation)
            .AsQueryable();
        if (cat.HasValue)    q = q.Where(o => o.ItemCategory == cat.Value);
        if (type.HasValue)   q = q.Where(o => o.Type         == type.Value);
        if (region.HasValue) q = q.Where(o => o.ServerRegion == region.Value);
        return await q.OrderByDescending(o => o.CreatedAt).Skip((page - 1) * size).Take(size).ToListAsync(ct);
    }

    public Task<List<Order>> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => db.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(Order order, CancellationToken ct = default) => await db.Orders.AddAsync(order, ct);
    public Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        // Só anexa entidades desanexadas — entidades rastreadas já são gerenciadas pelo change tracker.
        if (db.Entry(order).State == EntityState.Detached)
            db.Orders.Update(order);
        return Task.CompletedTask;
    }
}

public sealed class DealRepository(AlbionDbContext db) : IDealRepository
{
    public Task<Deal?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Deals.FirstOrDefaultAsync(d => d.DealId == id, ct);

    public Task<Deal?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default)
        => db.Deals.Include(d => d.Messages).FirstOrDefaultAsync(d => d.DealId == id, ct);

    public Task<List<Deal>> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => db.Deals.Where(d => d.BuyerId == userId || d.SellerId == userId).OrderByDescending(d => d.CreatedAt).ToListAsync(ct);

    public Task<List<Deal>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        => db.Deals.Where(d => d.OrderId == orderId).ToListAsync(ct);

    public async Task AddAsync(Deal deal, CancellationToken ct = default) => await db.Deals.AddAsync(deal, ct);
    public Task UpdateAsync(Deal deal, CancellationToken ct = default)
    {
        if (db.Entry(deal).State == EntityState.Detached)
            db.Deals.Update(deal);
        return Task.CompletedTask;
    }

    public async Task AddMessageAsync(Message message, CancellationToken ct = default)
        => await db.Set<Message>().AddAsync(message, ct);
}

public sealed class RatingRepository(AlbionDbContext db) : IRatingRepository
{
    public Task<List<Rating>> GetByUserIdAsync(string userId, CancellationToken ct = default)
        => db.Ratings.Where(r => r.RatedId == userId).OrderByDescending(r => r.CreatedAt).ToListAsync(ct);

    public Task<List<Rating>> GetByDealIdAsync(Guid dealId, CancellationToken ct = default)
        => db.Ratings.Where(r => r.DealId == dealId).ToListAsync(ct);

    public async Task AddAsync(Rating rating, CancellationToken ct = default) 
        => await db.Ratings.AddAsync(rating, ct);
}

// ── DI Extension ──────────────────────────────────────────────────────────────
public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AlbionDbContext>(o =>
            o.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(AlbionDbContext).Assembly.FullName)));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IDealRepository,  DealRepository>();
        services.AddScoped<IRatingRepository, RatingRepository>();
        services.AddScoped<IUnitOfWork>(sp  => sp.GetRequiredService<AlbionDbContext>());
        return services;
    }
}
