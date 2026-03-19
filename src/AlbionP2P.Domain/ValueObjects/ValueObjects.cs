using AlbionP2P.Domain.Exceptions;

namespace AlbionP2P.Domain.ValueObjects;

public enum ServerRegion    { Americas, Europe, Asia }
public enum ItemCategory    { Weapon, Armor, Gathering, Consumable, Material, Mount, Other }
public enum OrderType       { Buy, Sell }
public enum OrderStatusValue { Open, InNegotiation, Closed, Cancelled }
public enum DealStatusValue  { Pending, BuyerAccepted, SellerAccepted, BothAccepted, Rejected, Completed }
public enum RatingStatus    { Pending, Rated }

public sealed record Money
{
    public decimal Amount   { get; }
    public string  Currency => "Silver";
    public Money(decimal amount)
    {
        if (amount < 0) throw new DomainException("O valor em prata não pode ser negativo.");
        Amount = amount;
    }
    public override string ToString() => $"{Amount:N0} Silver";
}

public sealed record OrderStatus
{
    public OrderStatusValue Value { get; }

    //  ↓ Era 'v', agora é 'value' — EF Core faz match case-insensitive com a propriedade 'Value'
    private OrderStatus(OrderStatusValue value) => Value = value;

    public static OrderStatus Open()          => new(OrderStatusValue.Open);
    public static OrderStatus InNegotiation() => new(OrderStatusValue.InNegotiation);
    public static OrderStatus Closed()        => new(OrderStatusValue.Closed);
    public static OrderStatus Cancelled()     => new(OrderStatusValue.Cancelled);

    public bool IsOpen          => Value == OrderStatusValue.Open;
    public bool IsInNegotiation => Value == OrderStatusValue.InNegotiation;
    public override string ToString() => Value.ToString();
}
