using Microsoft.EntityFrameworkCore;
using PaxScan.Application.Abstractions;
using PaxScan.Application.Common;
using PaxScan.Domain.Devices;

namespace PaxScan.Application.Pairing;

public sealed record PairDeviceCommand(string PairingToken, string DeviceName, string Platform, string AppVersion);

public sealed record PairDeviceResult(Guid DeviceId, string DeviceSecret, Guid HubId);

public sealed class PairDeviceHandler(
    IHubDbContextFactory dbFactory,
    IWriteGate writeGate,
    ISecretService secrets,
    TimeProvider time)
{
    public async Task<Result<PairDeviceResult>> HandleAsync(PairDeviceCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.DeviceName) || command.DeviceName.Length > 100)
        {
            return Result<PairDeviceResult>.Failure(ErrorCodes.ValidationFailed, "Device name is required (max 100 characters).");
        }

        var now = time.GetUtcNow();
        var tokenHash = secrets.Hash(command.PairingToken);

        await using var gate = await writeGate.AcquireAsync(cancellationToken);
        await using var db = await dbFactory.CreateAsync(cancellationToken);

        var token = await db.PairingTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        if (token is null || !token.IsUsable(now))
        {
            return Result<PairDeviceResult>.Failure(ErrorCodes.PairingTokenInvalid, "Pairing code is invalid or expired. Show a new QR code.");
        }

        var settings = await db.Settings.SingleAsync(cancellationToken);
        var secret = secrets.Generate();
        var device = Device.Pair(
            Guid.CreateVersion7(now),
            command.DeviceName.Trim(),
            command.Platform,
            command.AppVersion,
            secrets.Hash(secret),
            now);

        token.Use(now);
        db.Devices.Add(device);
        await db.SaveChangesAsync(cancellationToken);

        return new PairDeviceResult(device.Id, secret, settings.HubId);
    }
}
