using PaxScan.Application.Abstractions;
using PaxScan.Application.Common;

namespace PaxScan.Application.Sync;

public sealed record HelloCommand(Guid DeviceId, long LastSeq, int PendingCount, DateTimeOffset DeviceTime);

public sealed record HelloResult(long ExpectedFromSeq, DateTimeOffset ServerTime);

public sealed class HelloHandler(IHubDbContextFactory dbFactory, IWriteGate writeGate, TimeProvider time)
{
    public async Task<Result<HelloResult>> HandleAsync(HelloCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var now = time.GetUtcNow();

        await using var gate = await writeGate.AcquireAsync(cancellationToken);
        await using var db = await dbFactory.CreateAsync(cancellationToken);

        var device = await db.Devices.FindAsync([command.DeviceId], cancellationToken);
        if (device is not { IsActive: true })
        {
            return Result<HelloResult>.Failure(ErrorCodes.DeviceRevoked, "Device is not paired.");
        }

        device.Hello(now, command.PendingCount, command.DeviceTime);
        await db.SaveChangesAsync(cancellationToken);
        return new HelloResult(device.LastSeq + 1, now);
    }
}
