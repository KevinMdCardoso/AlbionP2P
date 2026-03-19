using AlbionP2P.Domain.Events;
using AlbionP2P.Domain.Exceptions;
using AlbionP2P.Domain.ValueObjects;

namespace AlbionP2P.Domain.Aggregates;

public class Order
{
    private readonly List<IDomainEvent> _events = new();

    public Guid         OrderId      { get; private set; }
    public string       UserId       { get; private set; } = string.Empty;
    public string       ItemName     { get; private set; } = string.Empty;
    public ItemCategory ItemCategory { get; private set; }
    public int          Quantity     { get; private set; }
    public Money        UnitPrice    { get; private set; } = null!;
    public OrderType    Type         { get; private set; }
    public OrderStatus  Status       { get; private set; } = null!;
    public ServerRegion ServerRegion { get; private set; }
    public DateTime     CreatedAt    { get; private set; }
    public DateTime     UpdatedAt    { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();

    protected Order() { }

    public Order(string userId, string itemName, ItemCategory category,
                 int quantity, Money unitPrice, OrderType type, ServerRegion serverRegion)
    {
        if (string.IsNullOrWhiteSpace(itemName)) throw new DomainException("O nome do item é obrigatório.");
        if (quantity <= 0)                        throw new DomainException("A quantidade deve ser maior que zero.");

        OrderId      = Guid.NewGuid();
        UserId       = userId;
        ItemName     = itemName;
        ItemCategory = category;
        Quantity     = quantity;
        UnitPrice    = unitPrice;
        Type         = type;
        Status       = OrderStatus.Open();
        ServerRegion = serverRegion;
        CreatedAt    = UpdatedAt = DateTime.UtcNow;
    }

    public void StartNegotiation()
    {
        if (Status.Value == OrderStatusValue.Closed || Status.Value == OrderStatusValue.Cancelled)
            throw new DomainException("Apenas pedidos abertos podem entrar em negociação.");
        if (Status.IsOpen)
        {
            Status    = OrderStatus.InNegotiation();
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Close()
    {
        if (Status.Value == OrderStatusValue.Closed)    throw new DomainException("Pedido já está fechado.");
        if (Status.Value == OrderStatusValue.Cancelled) throw new DomainException("Pedido cancelado não pode ser fechado.");
        Status    = OrderStatus.Closed();
        UpdatedAt = DateTime.UtcNow;
        _events.Add(new OrderClosedEvent(OrderId, DateTime.UtcNow));
    }

    public void Cancel(string requestingUserId)
    {
        if (UserId != requestingUserId)                 throw new DomainException("Apenas o criador do pedido pode cancelá-lo.");
        if (Status.Value == OrderStatusValue.Closed)    throw new DomainException("Pedido fechado não pode ser cancelado.");
        if (Status.Value == OrderStatusValue.Cancelled) throw new DomainException("Pedido já está cancelado.");
        Status    = OrderStatus.Cancelled();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearDomainEvents() => _events.Clear();
}
