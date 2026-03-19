using AlbionP2P.Domain.Aggregates;
using AlbionP2P.Domain.ValueObjects;

namespace AlbionP2P.Domain.Interfaces;

public interface IOrderRepository
{
    Task<Order?>       GetByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<List<Order>>  GetRecentAsync(ItemCategory? category, OrderType? type, ServerRegion? region, int page, int pageSize, CancellationToken ct = default);
    Task<List<Order>>  GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task               AddAsync(Order order, CancellationToken ct = default);
    Task               UpdateAsync(Order order, CancellationToken ct = default);
}

public interface IDealRepository
{
    Task<Deal?>      GetByIdAsync(Guid dealId, CancellationToken ct = default);
    Task<Deal?>      GetByIdWithMessagesAsync(Guid dealId, CancellationToken ct = default);
    Task<List<Deal>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<List<Deal>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task             AddAsync(Deal deal, CancellationToken ct = default);
    Task             UpdateAsync(Deal deal, CancellationToken ct = default);
    Task             AddMessageAsync(Message message, CancellationToken ct = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IRatingRepository
{
    Task<List<Rating>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<List<Rating>> GetByDealIdAsync(Guid dealId, CancellationToken ct = default);
    Task AddAsync(Rating rating, CancellationToken ct = default);
}
