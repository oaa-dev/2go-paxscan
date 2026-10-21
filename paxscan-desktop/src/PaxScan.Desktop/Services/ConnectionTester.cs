using System.Net;
using System.Net.Http;
using PaxScan.Infrastructure.Security;

namespace PaxScan.Desktop.Services;

/// <summary>
/// Calls the hub's own advertised URL the way a phone would (pinned certificate). A 401 means
/// the address, port and firewall are fine.
/// </summary>
public sealed class ConnectionTester(CertificateService certificates)
{
    public async Task<(bool Ok, string Message)> TestAsync(string hubUrl, CancellationToken cancellationToken = default)
    {
        var fingerprint = certificates.GetFingerprint();
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, cert, _, _) =>
                cert is not null && CertificateService.Fingerprint(cert) == fingerprint,
        };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };

        try
        {
            using var response = await http.GetAsync(new Uri(new Uri(hubUrl), "/v1/sync/snapshot"), cancellationToken);
            return response.StatusCode == HttpStatusCode.Unauthorized
                ? (true, $"Reachable at {hubUrl}")
                : (false, $"Unexpected answer: HTTP {(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"Not reachable at {hubUrl}: {ex.Message}. Check the firewall rule and the advertised address in Settings.");
        }
        catch (TaskCanceledException)
        {
            return (false, $"Timed out reaching {hubUrl}. Check the firewall rule in Settings.");
        }
    }
}
