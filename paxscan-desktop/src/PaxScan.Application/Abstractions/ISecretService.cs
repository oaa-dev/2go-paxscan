namespace PaxScan.Application.Abstractions;

/// <summary>High-entropy random secrets (device secrets, pairing tokens) and their hashes.</summary>
public interface ISecretService
{
    string Generate();

    string Hash(string secret);

    /// <summary>Constant-time comparison.</summary>
    bool Verify(string secret, string hash);
}
