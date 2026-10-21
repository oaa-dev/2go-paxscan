namespace PaxScan.Domain.Sync;

/// <summary>Hub → Online upload state. <see cref="Skipped"/> is for updates the hub rejected.</summary>
public enum UploadStatus
{
    Pending,
    Uploading,
    Acked,
    Rejected,
    Skipped,
}
