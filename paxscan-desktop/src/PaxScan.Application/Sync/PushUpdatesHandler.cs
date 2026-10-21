using Microsoft.EntityFrameworkCore;
using PaxScan.Application.Abstractions;
using PaxScan.Application.Common;
using PaxScan.Domain.Sync;

namespace PaxScan.Application.Sync;

public sealed record PushResult(IReadOnlyList<ItemResult> Results, bool SeqGapDetected);

/// <summary>
/// Stores a device's status updates. Idempotent by update id; every returned result means
/// "durably stored on the hub" (including rejected items).
/// </summary>
public sealed class PushUpdatesHandler(IHubDbContextFactory dbFactory, IWriteGate writeGate, TimeProvider time)
{
    public const int MaxBatchSize = 200;

    public async Task<Result<PushResult>> HandleAsync(
        Guid deviceId,
        IReadOnlyList<DeviceStatusUpdate> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count is 0 or > MaxBatchSize)
        {
            return Result<PushResult>.Failure(ErrorCodes.ValidationFailed, $"A push must contain 1–{MaxBatchSize} items.");
        }

        if (items.Select(i => i.Id).Distinct().Count() != items.Count
            || items.Select(i => i.DeviceSeq).Distinct().Count() != items.Count)
        {
            return Result<PushResult>.Failure(ErrorCodes.ValidationFailed, "Item ids and device_seq values must be unique within a push.");
        }

        // Hub clock, not device clock, orders updates.
        var now = time.GetUtcNow();

        await using var gate = await writeGate.AcquireAsync(cancellationToken);
        await using var db = await dbFactory.CreateAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var device = await db.Devices.FindAsync([deviceId], cancellationToken);
        if (device is not { IsActive: true })
        {
            return Result<PushResult>.Failure(ErrorCodes.DeviceRevoked, "Device is not paired.");
        }

        var ids = items.Select(i => i.Id).ToList();
        var seqs = items.Select(i => i.DeviceSeq).ToList();
        var existing = await db.StatusUpdates.AsNoTracking()
            .Where(u => ids.Contains(u.Id) || (u.DeviceId == deviceId && seqs.Contains(u.DeviceSeq)))
            .Select(u => new { u.Id, u.DeviceSeq, u.BusinessStatus, u.ResultingVersion })
            .ToListAsync(cancellationToken);
        var existingById = existing.ToDictionary(e => e.Id);
        var takenSeqs = existing.Select(e => e.DeviceSeq).ToHashSet();

        var taskIds = items.Select(i => i.TaskId).Distinct().ToList();
        var tasks = await db.Tasks.Where(t => taskIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, cancellationToken);

        var results = new Dictionary<Guid, ItemResult>();
        var storedSeqs = new List<long>();

        foreach (var item in items.OrderBy(i => i.DeviceSeq))
        {
            if (existingById.TryGetValue(item.Id, out var already))
            {
                results[item.Id] = new ItemResult(item.Id, ItemResultCodes.AlreadyReceived, null, already.ResultingVersion);
                continue;
            }

            if (takenSeqs.Contains(item.DeviceSeq))
            {
                // Same seq, different id: a device bug. Not storable (unique seq), so not acked.
                results[item.Id] = new ItemResult(item.Id, ItemResultCodes.Rejected, "duplicate_seq", null);
                continue;
            }

            var outcome = tasks.TryGetValue(item.TaskId, out var task)
                ? task.ApplyStatusUpdate(item.FromStatus, item.ToStatus, item.BaseVersion, now)
                : StatusUpdateOutcome.Rejected(StatusUpdateRejectReason.TaskNotFound);

            db.StatusUpdates.Add(TaskStatusUpdate.Receive(item, deviceId, now, outcome));
            storedSeqs.Add(item.DeviceSeq);
            results[item.Id] = new ItemResult(item.Id, ToCode(outcome.BusinessStatus), outcome.RejectReason, outcome.ResultingVersion);
        }

        device.RecordPushed(storedSeqs, storedSeqs.Count, now);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PushResult(items.Select(i => results[i.Id]).ToList(), device.SeqGapDetected);
    }

    private static string ToCode(UpdateBusinessStatus status) => status switch
    {
        UpdateBusinessStatus.Accepted => ItemResultCodes.Accepted,
        UpdateBusinessStatus.Conflict => ItemResultCodes.Conflict,
        _ => ItemResultCodes.Rejected,
    };
}
