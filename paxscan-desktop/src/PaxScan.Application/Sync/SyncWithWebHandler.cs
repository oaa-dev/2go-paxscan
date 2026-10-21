using Microsoft.EntityFrameworkCore;
using PaxScan.Application.Abstractions;
using PaxScan.Application.Common;
using PaxScan.Domain.Sync;
using PaxScan.Domain.Tasks;

namespace PaxScan.Application.Sync;

public sealed record WebSyncSummary(int Uploaded, int Conflicts, int UploadRejected, int Downloaded, int Removed, string? DatasetVersion);

/// <summary>
/// Hub ↔ Online: upload pending status updates first, then download the task snapshot. A task
/// with updates still waiting for upload keeps its local status, so nothing is hidden.
/// </summary>
public sealed class SyncWithWebHandler(
    IHubDbContextFactory dbFactory,
    IWriteGate writeGate,
    IWebApiClient web,
    ISecretProtector protector,
    TimeProvider time)
{
    public const int UploadBatchSize = 200;

    private static readonly UploadStatus[] Unsent = [UploadStatus.Pending, UploadStatus.Uploading];

    public async Task<Result<WebSyncSummary>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var (connection, hubId) = await LoadConnectionAsync(cancellationToken);
        if (connection is null)
        {
            return Result<WebSyncSummary>.Failure(ErrorCodes.WebNotConfigured, "Set the Online URL and hub token in Settings first.");
        }

        try
        {
            var (uploaded, conflicts, rejected) = await UploadAsync(connection, hubId, cancellationToken);
            var snapshot = await web.GetTaskSnapshotAsync(connection, cancellationToken);

            var checksum = TaskChecksum.Compute(snapshot.Tasks.Select(t => (t.Id, t.Version)));
            if (!string.Equals(checksum, snapshot.Checksum, StringComparison.OrdinalIgnoreCase))
            {
                return Result<WebSyncSummary>.Failure(ErrorCodes.ChecksumMismatch, "Downloaded tasks failed verification; nothing was changed. Try again.");
            }

            var (downloaded, removed) = await ApplySnapshotAsync(snapshot, cancellationToken);
            return new WebSyncSummary(uploaded, conflicts, rejected, downloaded, removed, snapshot.DatasetVersion);
        }
        catch (WebApiException ex)
        {
            await ResetUploadingAsync(cancellationToken);
            return Result<WebSyncSummary>.Failure(ErrorCodes.WebUnavailable, ex.Message);
        }
    }

    private async Task<(WebConnection? Connection, Guid HubId)> LoadConnectionAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateAsync(cancellationToken);
        var settings = await db.Settings.AsNoTracking().SingleAsync(cancellationToken);
        if (settings.WebBaseUrl is null || settings.HubTokenProtected is null
            || !Uri.TryCreate(settings.WebBaseUrl, UriKind.Absolute, out var baseUrl))
        {
            return (null, settings.HubId);
        }

        return (new WebConnection(baseUrl, protector.Unprotect(settings.HubTokenProtected)), settings.HubId);
    }

    private async Task<(int Uploaded, int Conflicts, int Rejected)> UploadAsync(
        WebConnection connection,
        Guid hubId,
        CancellationToken cancellationToken)
    {
        int uploaded = 0, conflicts = 0, rejected = 0;

        // Each update is sent at most once per sync; one Online did not answer stays pending for the next sync.
        var attempted = new HashSet<Guid>();

        // A crash mid-upload leaves rows "uploading"; they are simply sent again (idempotent by id).
        while (true)
        {
            List<TaskStatusUpdate> batch;
            await using (var gate = await writeGate.AcquireAsync(cancellationToken))
            await using (var db = await dbFactory.CreateAsync(cancellationToken))
            {
                batch = await db.StatusUpdates
                    .Where(u => Unsent.Contains(u.UploadStatus) && !attempted.Contains(u.Id))
                    .OrderBy(u => u.HubReceivedAt).ThenBy(u => u.DeviceSeq)
                    .Take(UploadBatchSize)
                    .ToListAsync(cancellationToken);
                if (batch.Count == 0)
                {
                    break;
                }

                batch.ForEach(u =>
                {
                    u.MarkUploading();
                    attempted.Add(u.Id);
                });
                await db.SaveChangesAsync(cancellationToken);
            }

            var items = batch.Select(u => new WebStatusUpdate(
                u.Id, u.TaskId, u.FromStatus, u.ToStatus, u.WebBaseVersion ?? u.BaseVersion,
                u.DeviceId, u.DeviceSeq, u.DeviceCreatedAt, u.HubReceivedAt)).ToList();

            // Online answers per item; only an explicit result changes an item's state.
            var results = (await web.PushTaskUpdatesAsync(connection, hubId, items, cancellationToken))
                .ToDictionary(r => r.Id);

            var now = time.GetUtcNow();
            var progressed = 0;
            await using (var gate = await writeGate.AcquireAsync(cancellationToken))
            await using (var db = await dbFactory.CreateAsync(cancellationToken))
            {
                var batchIds = batch.Select(b => b.Id).ToList();
                var rows = await db.StatusUpdates.Where(u => batchIds.Contains(u.Id)).ToListAsync(cancellationToken);
                foreach (var row in rows)
                {
                    if (!results.TryGetValue(row.Id, out var result))
                    {
                        row.MarkUploadPending();
                        continue;
                    }

                    progressed++;
                    if (result.Result == ItemResultCodes.Rejected)
                    {
                        row.MarkUploadRejected(result.Reason);
                        rejected++;
                        continue;
                    }

                    row.MarkUploadAcked(now);
                    uploaded++;
                    if (result.Result == ItemResultCodes.Conflict)
                    {
                        conflicts++;
                    }
                }

                await db.SaveChangesAsync(cancellationToken);
            }

            if (progressed == 0)
            {
                throw new WebApiException("Online returned no results for the uploaded updates.");
            }
        }

        return (uploaded, conflicts, rejected);
    }

    private async Task<(int Downloaded, int Removed)> ApplySnapshotAsync(WebTaskSnapshot snapshot, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        await using var gate = await writeGate.AcquireAsync(cancellationToken);
        await using var db = await dbFactory.CreateAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var local = await db.Tasks.ToDictionaryAsync(t => t.Id, cancellationToken);
        var pendingTaskIds = (await db.StatusUpdates
                .Where(u => Unsent.Contains(u.UploadStatus))
                .Select(u => u.TaskId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (var fields in snapshot.Tasks)
        {
            if (local.Remove(fields.Id, out var task))
            {
                task.ReplaceFromWeb(fields, keepLocalStatus: pendingTaskIds.Contains(fields.Id));
            }
            else
            {
                db.Tasks.Add(TaskItem.FromWeb(fields));
            }
        }

        // Left in `local`: deleted on Online. Keep those with unsent updates until they upload.
        var removed = local.Values.Where(t => !pendingTaskIds.Contains(t.Id)).ToList();
        db.Tasks.RemoveRange(removed);

        var settings = await db.Settings.SingleAsync(cancellationToken);
        settings.RecordWebSync(snapshot.DatasetVersion, now);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return (snapshot.Tasks.Count, removed.Count);
    }

    private async Task ResetUploadingAsync(CancellationToken cancellationToken)
    {
        await using var gate = await writeGate.AcquireAsync(cancellationToken);
        await using var db = await dbFactory.CreateAsync(cancellationToken);
        var stuck = await db.StatusUpdates.Where(u => u.UploadStatus == UploadStatus.Uploading).ToListAsync(cancellationToken);
        stuck.ForEach(u => u.MarkUploadPending());
        await db.SaveChangesAsync(cancellationToken);
    }
}
