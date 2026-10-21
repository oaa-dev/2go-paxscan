using Microsoft.EntityFrameworkCore;
using PaxScan.Application.Abstractions;
using PaxScan.Application.Common;

namespace PaxScan.Application.Settings;

/// <summary>Leave <see cref="HubToken"/> null to keep the stored token.</summary>
public sealed record UpdateSettingsCommand(string? WebBaseUrl, string? HubToken, string? AdvertisedHost, int Port);

public sealed class UpdateSettingsHandler(IHubDbContextFactory dbFactory, IWriteGate writeGate, ISecretProtector protector)
{
    public async Task<Result<bool>> HandleAsync(UpdateSettingsCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!string.IsNullOrWhiteSpace(command.WebBaseUrl)
            && (!Uri.TryCreate(command.WebBaseUrl, UriKind.Absolute, out var url) || (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp)))
        {
            return Result<bool>.Failure(ErrorCodes.ValidationFailed, "Online URL must be an absolute http(s) URL.");
        }

        if (command.Port is < 1024 or > 65535)
        {
            return Result<bool>.Failure(ErrorCodes.ValidationFailed, "Port must be between 1024 and 65535.");
        }

        var protectedToken = string.IsNullOrWhiteSpace(command.HubToken) ? null : protector.Protect(command.HubToken.Trim());

        await using var gate = await writeGate.AcquireAsync(cancellationToken);
        await using var db = await dbFactory.CreateAsync(cancellationToken);
        var settings = await db.Settings.SingleAsync(cancellationToken);
        settings.UpdateConnection(command.WebBaseUrl, protectedToken, command.AdvertisedHost, command.Port);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
