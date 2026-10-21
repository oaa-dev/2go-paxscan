using PaxScan.Domain.Sync;

namespace PaxScan.Domain.Tasks;

/// <summary>
/// A task as known by the hub. Master fields come from Online; <see cref="Status"/> and
/// <see cref="Version"/> also move when the hub accepts a device's status update.
/// </summary>
public sealed class TaskItem
{
    private TaskItem()
    {
        Name = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public TaskItemType Type { get; private set; }

    public string? Description { get; private set; }

    public TaskItemStatus Status { get; private set; }

    /// <summary>Version mobile devices see and send back as <c>base_version</c>.</summary>
    public int Version { get; private set; }

    /// <summary>Online version last downloaded (informational).</summary>
    public int WebVersion { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static TaskItem FromWeb(WebTaskFields fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        var task = new TaskItem { Id = fields.Id };
        task.ReplaceFromWeb(fields, keepLocalStatus: false);
        return task;
    }

    /// <summary>
    /// Applies a downloaded Online row. When <paramref name="keepLocalStatus"/> is true (the task
    /// still has status updates waiting for upload) the local status and version are kept, so a
    /// download never hides a change Online has not seen yet.
    /// </summary>
    public void ReplaceFromWeb(WebTaskFields fields, bool keepLocalStatus)
    {
        ArgumentNullException.ThrowIfNull(fields);
        Name = fields.Name;
        Type = fields.Type;
        Description = fields.Description;
        WebVersion = fields.Version;
        CreatedAt = fields.CreatedAt;
        UpdatedAt = fields.UpdatedAt;

        if (!keepLocalStatus)
        {
            Status = fields.Status;
            Version = fields.Version;
        }
    }

    /// <summary>
    /// Applies an accepted device update: last received wins, but a stale base version is
    /// reported as a conflict.
    /// </summary>
    public StatusUpdateOutcome ApplyStatusUpdate(TaskItemStatus from, TaskItemStatus to, int baseVersion, DateTimeOffset now)
    {
        if (from == to)
        {
            return StatusUpdateOutcome.Rejected(StatusUpdateRejectReason.InvalidTransition);
        }

        var previousVersion = Version;
        var business = baseVersion == Version && from == Status
            ? UpdateBusinessStatus.Accepted
            : UpdateBusinessStatus.Conflict;

        Status = to;
        Version++;
        UpdatedAt = now;

        return StatusUpdateOutcome.Applied(business, previousVersion, Version);
    }
}

public sealed record WebTaskFields(
    Guid Id,
    string Name,
    TaskItemType Type,
    string? Description,
    TaskItemStatus Status,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
