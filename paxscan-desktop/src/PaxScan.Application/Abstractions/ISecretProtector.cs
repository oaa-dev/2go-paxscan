namespace PaxScan.Application.Abstractions;

/// <summary>Encrypts secrets at rest (DPAPI on Windows).</summary>
public interface ISecretProtector
{
    byte[] Protect(string plaintext);

    string Unprotect(byte[] protectedData);
}
