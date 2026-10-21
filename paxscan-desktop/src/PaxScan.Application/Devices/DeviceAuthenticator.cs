using Microsoft.EntityFrameworkCore;
using PaxScan.Application.Abstractions;

namespace PaxScan.Application.Devices;

/// <summary>Checks a device credential (<c>device_id:secret</c>). Revoked devices fail.</summary>
public sealed class DeviceAuthenticator(IHubDbContextFactory dbFactory, ISecretService secrets)
{
    public async Task<bool> AuthenticateAsync(Guid deviceId, string secret, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateAsync(cancellationToken);
        var device = await db.Devices.AsNoTracking().SingleOrDefaultAsync(d => d.Id == deviceId, cancellationToken);
        return device is { IsActive: true } && secrets.Verify(secret, device.SecretHash);
    }
}
