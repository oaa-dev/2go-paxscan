using PaxScan.Domain.Tasks;

namespace PaxScan.Domain.Sync;

/// <summary>
/// Immutable record of a status change made on a device. Corrections are new updates; only the
/// upload bookkeeping changes after creation.
/// </summary>
public sealed class TaskStatusUpdate
{
    private TaskStatusUpdate()
    {
    }

    public Guid Id { get; private set; }

    public Guid TaskId { get; private set; }

    public TaskItemStatus FromStatus { get; private set; }

    public TaskItemStatus ToStatus { get; private set; }

    /// <summary>Hub task version the device saw.</summary>
    public int BaseVersion { get; private set; }

    public Guid DeviceId { get; private set; }

    public long DeviceSeq { get; private set; }

    /// <summary>Device clock — informational only, never used to decide outcomes.</summary>
    public DateTimeOffset DeviceCreatedAt { get; private set; }

    public DateTimeOffset HubReceivedAt { get; private set; }

    public UpdateBusinessStatus BusinessStatus { get; private set; }

    public string? RejectReason { get; private set; }

    /// <summary>Task version after this update was applied on the hub.</summary>
    public int? ResultingVersion { get; private set; }

    /// <summary>Online version this update is based on (sent as <c>base_version</c> on upload).</summary>
    public int? WebBaseVersion { get; private set; }

    public UploadStatus UploadStatus { get; private set; }

    public DateTimeOffset? OnlineAckedAt { get; private set; }

    public string? UploadError { get; private set; }

    public static TaskStatusUpdate Receive(
        DeviceStatusUpdate item,
        Guid deviceId,
        DateTimeOffset hubReceivedAt,
        StatusUpdateOutcome outcome)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(outcome);

        var rejected = outcome.BusinessStatus == UpdateBusinessStatus.Rejected;
        return new TaskStatusUpdate
        {
            Id = item.Id,
            TaskId = item.TaskId,
            FromStatus = item.FromStatus,
            ToStatus = item.ToStatus,
            BaseVersion = item.BaseVersion,
            DeviceId = deviceId,
            DeviceSeq = item.DeviceSeq,
            DeviceCreatedAt = item.DeviceCreatedAt,
            HubReceivedAt = hubReceivedAt,
            BusinessStatus = outcome.BusinessStatus,
            RejectReason = outcome.RejectReason,
            ResultingVersion = outcome.ResultingVersion,
            WebBaseVersion = outcome.PreviousVersion,
            UploadStatus = rejected ? UploadStatus.Skipped : UploadStatus.Pending,
        };
    }

    public void MarkUploading() => UploadStatus = UploadStatus.Uploading;

    public void MarkUploadPending() => UploadStatus = UploadStatus.Pending;

    public void MarkUploadAcked(DateTimeOffset now)
    {
        UploadStatus = UploadStatus.Acked;
        OnlineAckedAt = now;
        UploadError = null;
    }

    public void MarkUploadRejected(string? reason)
    {
        UploadStatus = UploadStatus.Rejected;
        UploadError = reason ?? "rejected";
    }
}

/// <summary>A status update as pushed by a device.</summary>
public sealed record DeviceStatusUpdate(
    Guid Id,
    Guid TaskId,
    TaskItemStatus FromStatus,
    TaskItemStatus ToStatus,
    int BaseVersion,
    long DeviceSeq,
    DateTimeOffset DeviceCreatedAt);
