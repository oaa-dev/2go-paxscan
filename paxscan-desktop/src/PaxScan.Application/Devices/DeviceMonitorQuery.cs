using Microsoft.EntityFrameworkCore;
using PaxScan.Application.Abstractions;
using PaxScan.Domain.Devices;

namespace PaxScan.Application.Devices;

public sealed record DeviceRow(
    Guid Id,
    string Name,
    string Platform,
    string AppVersion,
    DeviceStatus Status,
    bool IsOnline,
    DateTimeOffset PairedAt,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset? LastSyncAt,
    long LastSeq,
    int PendingCount,
    bool SeqGapDetected,
    bool SyncRequested);

public sealed class DeviceMonitorQuery(IHubDbContextFactory dbFactory, TimeProvider time)
{
    public async Task<IReadOnlyList<DeviceRow>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        await using var db = await dbFactory.CreateAsync(cancellationToken);
        var devices = await db.Devices.AsNoTracking().ToListAsync(cancellationToken);

        return devices
            .OrderBy(d => d.Status)
            .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .Select(d => new DeviceRow(
                d.Id, d.Name, d.Platform, d.AppVersion, d.Status, d.IsOnline(now), d.PairedAt,
                d.LastSeenAt, d.LastSyncAt, d.LastSeq, d.PendingCount, d.SeqGapDetected, d.SyncRequested))
            .ToList();
    }
}
