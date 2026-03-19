using AlbionP2P.Domain.Events;
using AlbionP2P.Domain.Exceptions;
using AlbionP2P.Domain.ValueObjects;

namespace AlbionP2P.Domain.Aggregates;

public class Deal
{
    private readonly List<IDomainEvent> _events   = new();
    private readonly List<Message>      _messages = new();
    private readonly List<Rating>       _ratings  = new();

    public Guid           DealId        { get; private set; }
    public Guid           OrderId       { get; private set; }
    public string         BuyerId       { get; private set; } = string.Empty;
    public string         SellerId      { get; private set; } = string.Empty;
    public Money          ProposedPrice { get; private set; } = null!;
    public DealStatusValue Status       { get; private set; }
    public bool           BuyerConfirmed { get; private set; }
    public bool           SellerConfirmed { get; private set; }
    public DateTime        CreatedAt    { get; private set; }
    public DateTime        UpdatedAt    { get; private set; }

    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();
    public IReadOnlyCollection<Rating> Ratings => _ratings.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();

    protected Deal() { }

    public Deal(Guid orderId, string buyerId, string sellerId, Money proposedPrice)
    {
        if (buyerId == sellerId) throw new DomainException("Comprador e vendedor não podem ser o mesmo usuário.");
        DealId        = Guid.NewGuid();
        OrderId       = orderId;
        BuyerId       = buyerId;
        SellerId      = sellerId;
        ProposedPrice = proposedPrice;
        Status        = DealStatusValue.Pending;
        BuyerConfirmed = false;
        SellerConfirmed = false;
        CreatedAt     = UpdatedAt = DateTime.UtcNow;
    }

    public void AcceptBySeller()
    {
        if (Status != DealStatusValue.Pending) 
            throw new DomainException("Apenas propostas pendentes podem ser aceitas pelo vendedor.");
        SellerConfirmed = true;
        Status = DealStatusValue.SellerAccepted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AcceptByBuyer()
    {
        if (Status != DealStatusValue.SellerAccepted) 
            throw new DomainException("O vendedor deve aceitar primeiro.");
        BuyerConfirmed = true;
        Status = DealStatusValue.BothAccepted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string userId)
    {
        if (userId != SellerId && userId != BuyerId)
            throw new DomainException("Apenas participantes podem rejeitar.");
        if (Status == DealStatusValue.Rejected || Status == DealStatusValue.Completed)
            throw new DomainException("Esta negociação já foi encerrada.");
        Status = DealStatusValue.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != DealStatusValue.BothAccepted)
            throw new DomainException("Apenas deals aceitos por ambos podem ser concluídos.");
        Status = DealStatusValue.Completed;
        UpdatedAt = DateTime.UtcNow;
        _events.Add(new DealCompletedEvent(DealId, OrderId, Guid.Parse(BuyerId), Guid.Parse(SellerId), DateTime.UtcNow));
    }

    public void AddRating(string raterId, string ratedId, int stars, string comment)
    {
        if (Status != DealStatusValue.Completed)
            throw new DomainException("Apenas deals completados podem ser avaliados.");
        if (raterId != BuyerId && raterId != SellerId)
            throw new DomainException("Apenas participantes podem avaliar.");
        if (raterId == ratedId)
            throw new DomainException("Você não pode avaliar a si mesmo.");

        var existingRating = _ratings.FirstOrDefault(r => r.RaterId == raterId && r.RatedId == ratedId);
        if (existingRating != null)
            throw new DomainException("Você já avaliou este usuário.");

        var rating = new Rating(DealId, raterId, ratedId, stars, comment);
        _ratings.Add(rating);
    }

    public Message AddMessage(string senderId, string content)
    {
        if (Status is DealStatusValue.Rejected or DealStatusValue.Completed)
            throw new DomainException("Não é possível enviar mensagens em um deal encerrado.");
        if (senderId != BuyerId && senderId != SellerId)
            throw new DomainException("Apenas participantes do deal podem enviar mensagens.");
        var msg = new Message(DealId, senderId, content);
        _messages.Add(msg);
        return msg;
    }

    public void ClearDomainEvents() => _events.Clear();
}

public class Message
{
    public Guid     MessageId { get; private set; }
    public Guid     DealId    { get; private set; }
    public string   SenderId  { get; private set; } = string.Empty;
    public string   Content   { get; private set; } = string.Empty;
    public DateTime SentAt    { get; private set; }

    protected Message() { }

    public Message(Guid dealId, string senderId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("O conteúdo da mensagem não pode ser vazio.");
        MessageId = Guid.NewGuid();
        DealId    = dealId;
        SenderId  = senderId;
        Content   = content;
        SentAt    = DateTime.UtcNow;
    }
}
