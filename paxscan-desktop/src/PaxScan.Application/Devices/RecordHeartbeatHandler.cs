using PaxScan.Application.Abstractions;
using PaxScan.Application.Common;

namespace PaxScan.Application.Devices;

public sealed record PingResult(bool SyncRequested, DateTimeOffset ServerTime);

public sealed class RecordHeartbeatHandler(IHubDbContextFactory dbFactory, IWriteGate writeGate, TimeProvider time)
{
    public async Task<Result<PingResult>> HandleAsync(Guid deviceId, int pendingCount, CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        await using var gate = await writeGate.AcquireAsync(cancellationToken);
        await using var db = await dbFactory.CreateAsync(cancellationToken);

        var device = await db.Devices.FindAsync([deviceId], cancellationToken);
        if (device is not { IsActive: true })
        {
            return Result<PingResult>.Failure(ErrorCodes.DeviceRevoked, "Device is not paired.");
        }

        device.Heartbeat(now, pendingCount);
        await db.SaveChangesAsync(cancellationToken);
        return new PingResult(device.SyncRequested, now);
    }
}
