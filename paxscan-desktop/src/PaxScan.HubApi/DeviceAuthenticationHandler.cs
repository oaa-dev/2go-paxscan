using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaxScan.Application.Devices;

namespace PaxScan.HubApi;

/// <summary>Authenticates <c>Authorization: Device &lt;device_id&gt;:&lt;secret&gt;</c>.</summary>
public sealed class DeviceAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    DeviceAuthenticator authenticator)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Device";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith(SchemeName + " ", StringComparison.Ordinal))
        {
            return AuthenticateResult.NoResult();
        }

        var credential = header[(SchemeName.Length + 1)..].Trim();
        var separator = credential.IndexOf(':', StringComparison.Ordinal);
        if (separator <= 0 || !Guid.TryParse(credential.AsSpan(0, separator), out var deviceId))
        {
            return AuthenticateResult.Fail("Malformed device credential.");
        }

        var secret = credential[(separator + 1)..];
        if (!await authenticator.AuthenticateAsync(deviceId, secret, Context.RequestAborted))
        {
            return AuthenticateResult.Fail("Unknown or revoked device.");
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, deviceId.ToString())], SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}

internal static class DeviceClaims
{
    public static Guid DeviceId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
