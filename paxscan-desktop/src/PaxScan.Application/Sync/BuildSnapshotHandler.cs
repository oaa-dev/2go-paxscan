using Microsoft.EntityFrameworkCore;
using PaxScan.Application.Abstractions;
using PaxScan.Domain.Tasks;

namespace PaxScan.Application.Sync;

public sealed record SnapshotResult(string? DatasetVersion, IReadOnlyList<TaskItem> Tasks);

/// <summary>Tasks sent to a device. Demo: every device gets all tasks (real app filters by role).</summary>
public sealed class BuildSnapshotHandler(IHubDbContextFactory dbFactory)
{
    public async Task<SnapshotResult> HandleAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateAsync(cancellationToken);
        var settings = await db.Settings.AsNoTracking().SingleAsync(cancellationToken);
        var tasks = await db.Tasks.AsNoTracking().ToListAsync(cancellationToken);
        return new SnapshotResult(
            settings.DatasetVersion,
            tasks.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList());
    }
}
