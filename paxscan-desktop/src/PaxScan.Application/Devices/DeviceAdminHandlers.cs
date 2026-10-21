using Microsoft.EntityFrameworkCore;
using PaxScan.Application.Abstractions;

namespace PaxScan.Application.Devices;

/// <summary>Operator actions from the desktop Devices screen.</summary>
public sealed class DeviceAdminHandlers(IHubDbContextFactory dbFactory, IWriteGate writeGate)
{
    /// <summary>Asks one device (or all active devices when <paramref name="deviceId"/> is null) to sync.</summary>
    public async Task<int> RequestSyncAsync(Guid? deviceId, CancellationToken cancellationToken = default)
    {
        await using var gate = await writeGate.AcquireAsync(cancellationToken);
        await using var db = await dbFactory.CreateAsync(cancellationToken);

        var devices = await db.Devices
            .Where(d => deviceId == null || d.Id == deviceId)
            .ToListAsync(cancellationToken);
        var requested = devices.Where(d => d.IsActive).ToList();
        requested.ForEach(d => d.RequestSync());
        await db.SaveChangesAsync(cancellationToken);
        return requested.Count;
    }

    public async Task<bool> RevokeAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        await using var gate = await writeGate.AcquireAsync(cancellationToken);
        await using var db = await dbFactory.CreateAsync(cancellationToken);

        var device = await db.Devices.FindAsync([deviceId], cancellationToken);
        if (device is null)
        {
            return false;
        }

        device.Revoke();
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
