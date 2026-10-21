using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using PaxScan.Application.Abstractions;
using PaxScan.Infrastructure.Networking;

namespace PaxScan.Infrastructure.Security;

/// <summary>
/// The hub's self-signed TLS certificate. Phones pin its SHA-256 fingerprint (from the QR code),
/// so the hostname in the SAN does not need to match; it is filled in for convenience only.
/// Regenerating the certificate means every phone must pair again.
/// </summary>
public sealed class CertificateService(IOptions<HubOptions> options, ISecretProtector protector, TimeProvider time)
{
    private readonly Lock _lock = new();
    private X509Certificate2? _certificate;

    private string PfxPath => Path.Combine(options.Value.DataDirectory, "hub-cert.pfx");

    private string PasswordPath => Path.Combine(options.Value.DataDirectory, "hub-cert.key");

    public X509Certificate2 GetOrCreate()
    {
        lock (_lock)
        {
            if (_certificate is not null)
            {
                return _certificate;
            }

            Directory.CreateDirectory(options.Value.DataDirectory);
            if (!File.Exists(PfxPath) || !File.Exists(PasswordPath))
            {
                CreateAndSave();
            }

            var password = protector.Unprotect(File.ReadAllBytes(PasswordPath));
            _certificate = X509CertificateLoader.LoadPkcs12FromFile(PfxPath, password);
            return _certificate;
        }
    }

    /// <summary>Lowercase hex SHA-256 of the DER certificate — what phones pin.</summary>
    public string GetFingerprint() => Fingerprint(GetOrCreate());

    public static string Fingerprint(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        return Convert.ToHexStringLower(SHA256.HashData(certificate.RawData));
    }

    private void CreateAndSave()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=PaxScan Hub", key, HashAlgorithmName.SHA256);

        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        san.AddIpAddress(IPAddress.Loopback);
        foreach (var address in LanAddresses.GetIPv4Addresses().Append(LanAddresses.MobileHotspotAddress).Distinct())
        {
            san.AddIpAddress(IPAddress.Parse(address));
        }

        request.CertificateExtensions.Add(san.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], false));

        var now = time.GetUtcNow();
        using var certificate = request.CreateSelfSigned(now.AddDays(-1), now.AddYears(5));

        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        File.WriteAllBytes(PfxPath, certificate.Export(X509ContentType.Pkcs12, password));
        File.WriteAllBytes(PasswordPath, protector.Protect(password));
    }
}
