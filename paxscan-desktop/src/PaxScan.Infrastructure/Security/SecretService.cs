using System.Security.Cryptography;
using System.Text;
using PaxScan.Application.Abstractions;

namespace PaxScan.Infrastructure.Security;

/// <summary>
/// 256-bit random secrets, base64url. A fast SHA-256 hash is enough here: the secrets are random,
/// not user passwords, so there is nothing to brute-force.
/// </summary>
internal sealed class SecretService : ISecretService
{
    public string Generate() => Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public string Hash(string secret)
    {
        ArgumentNullException.ThrowIfNull(secret);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
    }

    public bool Verify(string secret, string hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        var actual = Encoding.ASCII.GetBytes(Hash(secret));
        var expected = Encoding.ASCII.GetBytes(hash);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
