using PaxScan.Application.Abstractions;
using PaxScan.Domain.Pairing;

namespace PaxScan.Application.Pairing;

public sealed record PairingTokenIssued(string Token, DateTimeOffset ExpiresAt);

/// <summary>Issues a single-use token for the pairing QR code.</summary>
public sealed class CreatePairingTokenHandler(
    IHubDbContextFactory dbFactory,
    IWriteGate writeGate,
    ISecretService secrets,
    TimeProvider time)
{
    public async Task<PairingTokenIssued> HandleAsync(CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        var token = secrets.Generate();
        var pairingToken = PairingToken.Issue(secrets.Hash(token), now);

        await using var gate = await writeGate.AcquireAsync(cancellationToken);
        await using var db = await dbFactory.CreateAsync(cancellationToken);
        db.PairingTokens.Add(pairingToken);
        await db.SaveChangesAsync(cancellationToken);

        return new PairingTokenIssued(token, pairingToken.ExpiresAt);
    }
}
