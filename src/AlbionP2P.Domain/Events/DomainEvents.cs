namespace AlbionP2P.Domain.Events;

public interface IDomainEvent { DateTime OccurredAt { get; } }

public sealed record OrderClosedEvent(Guid OrderId, DateTime OccurredAt) : IDomainEvent;
public sealed record DealCompletedEvent(Guid DealId, Guid OrderId, Guid BuyerId, Guid SellerId, DateTime OccurredAt) : IDomainEvent;
