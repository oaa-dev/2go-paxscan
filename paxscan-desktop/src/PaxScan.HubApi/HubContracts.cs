using PaxScan.Application.Sync;
using PaxScan.Domain.Tasks;

namespace PaxScan.HubApi;

// Wire shapes for the hub API (contracts/openapi.yaml, tag hub-device). Serialized snake_case.

public sealed record PairRequest(string? PairingToken, string? DeviceName, string? Platform, string? AppVersion);

public sealed record PairResponse(Guid DeviceId, string DeviceSecret, Guid HubId);

public sealed record PingRequest(int PendingCount, long LastSeq);

public sealed record PingResponse(bool SyncRequested, DateTimeOffset ServerTime);

public sealed record HelloRequest(long LastSeq, int PendingCount, DateTimeOffset DeviceTime);

public sealed record HelloResponse(long ExpectedFromSeq, DateTimeOffset ServerTime);

public sealed record PushItem(
    Guid Id,
    Guid TaskId,
    TaskItemStatus FromStatus,
    TaskItemStatus ToStatus,
    int BaseVersion,
    long DeviceSeq,
    DateTimeOffset DeviceCreatedAt);

public sealed record PushRequest(IReadOnlyList<PushItem>? Items);

public sealed record PushResponse(IReadOnlyList<ItemResult> Results, bool SeqGapDetected);

public sealed record TaskDto(
    Guid Id,
    string Name,
    TaskItemType Type,
    string? Description,
    TaskItemStatus Status,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static TaskDto From(TaskItem task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return new(task.Id, task.Name, task.Type, task.Description, task.Status, task.Version, task.CreatedAt, task.UpdatedAt);
    }
}

public sealed record SnapshotResponse(string? DatasetVersion, IReadOnlyList<TaskDto> Tasks);
