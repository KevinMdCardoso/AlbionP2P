using AlbionP2P.Domain.ValueObjects;

namespace AlbionP2P.Application.DTOs;

// ── Envelope padrão ───────────────────────────────────────────────────────────
public sealed record ApiResponse<T>(bool Success, T? Data, string? Error)
{
    public static ApiResponse<T> Ok(T data)        => new(true, data, null);
    public static ApiResponse<T> Fail(string error) => new(false, default, error);
}

// ── Auth ──────────────────────────────────────────────────────────────────────
public sealed record RegisterRequest(string Email, string Password, string AlbionNick, ServerRegion ServerRegion);
public sealed record LoginRequest(string Email, string Password);
public sealed record UserDto(string Id, string Email, string AlbionNick, ServerRegion ServerRegion, int Reputation);

// ── Orders ────────────────────────────────────────────────────────────────────
public sealed record CreateOrderRequest(string ItemName, ItemCategory Category, int Quantity, decimal UnitPrice, OrderType Type, ServerRegion ServerRegion);
public sealed record OrderDto(Guid OrderId, string UserId, string AlbionNick, string ItemName, ItemCategory Category, int Quantity, decimal UnitPrice, OrderType Type, string Status, ServerRegion ServerRegion, DateTime CreatedAt);
public sealed record OrderListDto(List<OrderDto> Items, int TotalCount, int Page, int PageSize);

// ── Deals ─────────────────────────────────────────────────────────────────────
public sealed record CreateDealRequest(Guid OrderId, decimal ProposedPrice);
public sealed record DealDto(Guid DealId, Guid OrderId, string BuyerId, string SellerId, decimal ProposedPrice, string Status, bool BuyerConfirmed, bool SellerConfirmed, DateTime CreatedAt);

// ── Messages ──────────────────────────────────────────────────────────────────
public sealed record MessageDto(Guid MessageId, string SenderId, string AlbionNick, string Content, DateTime SentAt);

// ── Ratings ───────────────────────────────────────────────────────────────────
public sealed record RatingDto(Guid Id, Guid DealId, string RaterId, string RatedId, int Stars, string Comment, DateTime CreatedAt);
public sealed record CreateRatingRequest(Guid DealId, int Stars, string Comment);
public sealed record UserStatsDto(string UserId, string Email, string AlbionNick, ServerRegion ServerRegion, int Reputation, double AverageRating, int TotalRatings);
public sealed record UserProfileDto(UserDto User, List<RatingDto> Ratings, double AverageRating);
