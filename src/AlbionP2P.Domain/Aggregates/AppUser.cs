using AlbionP2P.Domain.Exceptions;
using AlbionP2P.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace AlbionP2P.Domain.Aggregates;

public class AppUser : IdentityUser
{
    public string       AlbionNick   { get; private set; } = string.Empty;
    public ServerRegion ServerRegion { get; private set; }
    public int          Reputation   { get; private set; }
    public DateTime     CreatedAt    { get; private set; }

    protected AppUser() { }

    public AppUser(string email, string albionNick, ServerRegion serverRegion)
    {
        if (string.IsNullOrWhiteSpace(albionNick))
            throw new DomainException("O nick do Albion é obrigatório.");
        UserName     = email;
        Email        = email;
        AlbionNick   = albionNick;
        ServerRegion = serverRegion;
        Reputation   = 0;
        CreatedAt    = DateTime.UtcNow;
    }

    public void AddReputation(int points)
    {
        if (points <= 0) throw new DomainException("Os pontos de reputação devem ser positivos.");
        Reputation += points;
    }
}
