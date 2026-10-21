using System.Text.Json;
using PaxScan.Application.Common;
using PaxScan.Application.Pairing;
using PaxScan.Infrastructure.Networking;
using PaxScan.Infrastructure.Security;
using QRCoder;

namespace PaxScan.Infrastructure.Pairing;

public sealed record QrPayload(string Json, byte[] Png, string HubUrl, DateTimeOffset ExpiresAt);

/// <summary>Builds the pairing QR: <c>{v, hub_url, cert_sha256, pairing_token, hub_id}</c>.</summary>
public sealed class QrPayloadFactory(CertificateService certificates)
{
    public QrPayload Create(PairingTokenIssued token, Guid hubId, string? advertisedHost, int port)
    {
        ArgumentNullException.ThrowIfNull(token);
        var lan = LanAddresses.GetIPv4Addresses();
        var host = advertisedHost ?? (lan.Count > 0 ? lan[0] : LanAddresses.MobileHotspotAddress);
        var hubUrl = $"https://{host}:{port}";

        var json = JsonSerializer.Serialize(
            new QrContent(1, hubUrl, certificates.GetFingerprint(), token.Token, hubId),
            WireJson.Options);

        using var data = QRCodeGenerator.GenerateQrCode(json, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data).GetGraphic(10);

        return new QrPayload(json, png, hubUrl, token.ExpiresAt);
    }

    private sealed record QrContent(int V, string HubUrl, string CertSha256, string PairingToken, Guid HubId);
}
