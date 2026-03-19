namespace AlbionP2P.Web.Models;

public enum ServerRegion { Americas, Europe, Asia }
public enum ItemCategory { Weapon, Armor, Gathering, Consumable, Material, Mount, Other }
public enum OrderType    { Buy, Sell }

public class ApiResponse<T> { public bool Success { get; set; } public T? Data { get; set; } public string? Error { get; set; } }

public class UserDto  { public string Id { get; set; } = ""; public string Email { get; set; } = ""; public string AlbionNick { get; set; } = ""; public ServerRegion ServerRegion { get; set; } public int Reputation { get; set; } }
public class RegisterRequest { public string Email { get; set; } = ""; public string Password { get; set; } = ""; public string AlbionNick { get; set; } = ""; public ServerRegion ServerRegion { get; set; } }
public class LoginRequest    { public string Email { get; set; } = ""; public string Password { get; set; } = ""; }

public class OrderDto
{
    public Guid         OrderId      { get; set; }
    public string       UserId       { get; set; } = "";
    public string       AlbionNick   { get; set; } = "";
    public string       ItemName     { get; set; } = "";
    public ItemCategory Category     { get; set; }
    public int          Quantity     { get; set; }
    public decimal      UnitPrice    { get; set; }
    public OrderType    Type         { get; set; }
    public string       Status       { get; set; } = "";
    public ServerRegion ServerRegion { get; set; }
    public DateTime     CreatedAt    { get; set; }
}
public class OrderListDto { public List<OrderDto> Items { get; set; } = new(); public int TotalCount { get; set; } public int Page { get; set; } public int PageSize { get; set; } }
public class CreateOrderRequest { public string ItemName { get; set; } = ""; public ItemCategory Category { get; set; } public int Quantity { get; set; } = 1; public decimal UnitPrice { get; set; } public OrderType Type { get; set; } public ServerRegion ServerRegion { get; set; } }

public class DealDto { public Guid DealId { get; set; } public Guid OrderId { get; set; } public string BuyerId { get; set; } = ""; public string SellerId { get; set; } = ""; public decimal ProposedPrice { get; set; } public string Status { get; set; } = ""; public bool BuyerConfirmed { get; set; } public bool SellerConfirmed { get; set; } public DateTime CreatedAt { get; set; } }
public class CreateDealRequest { public Guid OrderId { get; set; } public decimal ProposedPrice { get; set; } }

public class RatingDto { public Guid Id { get; set; } public Guid DealId { get; set; } public string RaterId { get; set; } = ""; public string RatedId { get; set; } = ""; public int Stars { get; set; } public string Comment { get; set; } = ""; public DateTime CreatedAt { get; set; } }
public class CreateRatingRequest { public Guid DealId { get; set; } public int Stars { get; set; } public string Comment { get; set; } = ""; }
public class UserProfileDto { public UserDto? User { get; set; } public List<RatingDto> Ratings { get; set; } = new(); public double AverageRating { get; set; } }

public class MessageDto { public Guid MessageId { get; set; } public string SenderId { get; set; } = ""; public string AlbionNick { get; set; } = ""; public string Content { get; set; } = ""; public DateTime SentAt { get; set; } }
