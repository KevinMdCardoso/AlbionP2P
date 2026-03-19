using AlbionP2P.Domain.Exceptions;

namespace AlbionP2P.Domain.Aggregates;

public class Rating
{
    public Guid Id { get; private set; }
    public Guid DealId { get; private set; }
    public string RaterId { get; private set; } = string.Empty;        // Quem avaliou
    public string RatedId { get; private set; } = string.Empty;        // Quem foi avaliado
    public int Stars { get; private set; }                             // 1-5
    public string Comment { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    protected Rating() { }

    public Rating(Guid dealId, string raterId, string ratedId, int stars, string comment)
    {
        if (stars < 1 || stars > 5) 
            throw new DomainException("A avaliação deve ser entre 1 e 5 estrelas.");
        if (string.IsNullOrWhiteSpace(comment))
            throw new DomainException("O comentário não pode ser vazio.");
        
        Id = Guid.NewGuid();
        DealId = dealId;
        RaterId = raterId;
        RatedId = ratedId;
        Stars = stars;
        Comment = comment;
        CreatedAt = DateTime.UtcNow;
    }
}
