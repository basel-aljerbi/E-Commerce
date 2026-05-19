namespace E_Commerce.API.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = default!;
    public string TokenHash { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;

    // Token family for rotation/reuse detection
    public Guid FamilyId { get; set; } = Guid.NewGuid();
    public int? PreviousTokenId { get; set; }
    public RefreshToken? PreviousToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }

    public ICollection<RefreshToken> DescendantTokens { get; set; } = new List<RefreshToken>();
}
