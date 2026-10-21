namespace PaxScan.Domain.Devices;

/// <summary>A paired mobile device.</summary>
public sealed class Device
{
    /// <summary>A device is shown as online when it pinged within this window.</summary>
    public static readonly TimeSpan OnlineWindow = TimeSpan.FromSeconds(60);

    private Device()
    {
        Name = string.Empty;
        Platform = string.Empty;
        AppVersion = string.Empty;
        SecretHash = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Platform { get; private set; }

    public string AppVersion { get; private set; }

    public string SecretHash { get; private set; }

    public DeviceStatus Status { get; private set; }

    public DateTimeOffset PairedAt { get; private set; }

    public DateTimeOffset? LastSeenAt { get; private set; }

    public DateTimeOffset? LastSyncAt { get; private set; }

    /// <summary>Highest device_seq stored on the hub.</summary>
    public long LastSeq { get; private set; }

    /// <summary>Outbox items the device reported as not yet acked.</summary>
    public int PendingCount { get; private set; }

    public bool SeqGapDetected { get; private set; }

    /// <summary>Hub clock − device clock at the last handshake (informational).</summary>
    public long? ClockOffsetMs { get; private set; }

    /// <summary>Set by the desktop operator; the device sees it on its next ping and syncs.</summary>
    public bool SyncRequested { get; private set; }

    public bool IsActive => Status == DeviceStatus.Active;

    public static Device Pair(Guid id, string name, string platform, string appVersion, string secretHash, DateTimeOffset now) =>
        new()
        {
            Id = id,
            Name = name,
            Platform = platform,
            AppVersion = appVersion,
            SecretHash = secretHash,
            Status = DeviceStatus.Active,
            PairedAt = now,
            LastSeenAt = now,
        };

    public bool IsOnline(DateTimeOffset now) =>
        IsActive && LastSeenAt is { } seen && now - seen <= OnlineWindow;

    public void Heartbeat(DateTimeOffset now, int pendingCount)
    {
        LastSeenAt = now;
        PendingCount = Math.Max(0, pendingCount);
    }

    /// <summary>Start of a sync session; clears any pending sync request.</summary>
    public void Hello(DateTimeOffset now, int pendingCount, DateTimeOffset deviceTime)
    {
        Heartbeat(now, pendingCount);
        ClockOffsetMs = (long)(now - deviceTime).TotalMilliseconds;
        SyncRequested = false;
    }

    /// <summary>Records stored sequence numbers and flags any gap (missing data on the device).</summary>
    public void RecordPushed(IEnumerable<long> storedSeqs, int storedCount, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(storedSeqs);
        var expected = LastSeq + 1;
        foreach (var seq in storedSeqs.Order())
        {
            if (seq > expected)
            {
                SeqGapDetected = true;
            }

            expected = Math.Max(expected, seq + 1);
        }

        LastSeq = Math.Max(LastSeq, expected - 1);
        PendingCount = Math.Max(0, PendingCount - storedCount);
        LastSeenAt = now;
        LastSyncAt = now;
    }

    public void RequestSync()
    {
        if (IsActive)
        {
            SyncRequested = true;
        }
    }

    public void Revoke()
    {
        Status = DeviceStatus.Revoked;
        SyncRequested = false;
    }
}
