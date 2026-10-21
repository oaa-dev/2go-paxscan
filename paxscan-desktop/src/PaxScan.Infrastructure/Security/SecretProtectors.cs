using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using PaxScan.Application.Abstractions;

namespace PaxScan.Infrastructure.Security;

/// <summary>Windows DPAPI, CurrentUser scope: only this Windows account can decrypt.</summary>
[SupportedOSPlatform("windows")]
internal sealed class DpapiSecretProtector : ISecretProtector
{
    private static readonly byte[] Entropy = "PaxScan.Hub.v1"u8.ToArray();

    public byte[] Protect(string plaintext) =>
        ProtectedData.Protect(Encoding.UTF8.GetBytes(plaintext), Entropy, DataProtectionScope.CurrentUser);

    public string Unprotect(byte[] protectedData) =>
        Encoding.UTF8.GetString(ProtectedData.Unprotect(protectedData, Entropy, DataProtectionScope.CurrentUser));
}

/// <summary>
/// Non-Windows development/test fallback. It does NOT encrypt; the hub only ships on Windows.
/// </summary>
internal sealed class DevelopmentSecretProtector : ISecretProtector
{
    public byte[] Protect(string plaintext) => Encoding.UTF8.GetBytes(plaintext);

    public string Unprotect(byte[] protectedData) => Encoding.UTF8.GetString(protectedData);
}
