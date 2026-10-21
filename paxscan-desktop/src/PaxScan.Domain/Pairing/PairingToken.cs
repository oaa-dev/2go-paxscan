namespace PaxScan.Domain.Pairing;

/// <summary>One-time pairing token shown in the QR code. Only its hash is stored.</summary>
public sealed class PairingToken
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private PairingToken()
    {
        TokenHash = string.Empty;
    }

    public Guid Id { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public static PairingToken Issue(string tokenHash, DateTimeOffset now) =>
        new() { Id = Guid.CreateVersion7(now), TokenHash = tokenHash, ExpiresAt = now + Lifetime };

    public bool IsUsable(DateTimeOffset now) => UsedAt is null && now < ExpiresAt;

    public void Use(DateTimeOffset now) => UsedAt = now;
}
