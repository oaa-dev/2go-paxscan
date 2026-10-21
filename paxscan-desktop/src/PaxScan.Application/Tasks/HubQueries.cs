using Microsoft.EntityFrameworkCore;
using PaxScan.Application.Abstractions;
using PaxScan.Domain.Settings;
using PaxScan.Domain.Sync;
using PaxScan.Domain.Tasks;

namespace PaxScan.Application.Tasks;

/// <summary>Read models for the desktop screens.</summary>
public sealed class HubQueries(IHubDbContextFactory dbFactory)
{
    public async Task<IReadOnlyList<TaskItem>> ListTasksAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateAsync(cancellationToken);
        var tasks = await db.Tasks.AsNoTracking().ToListAsync(cancellationToken);
        return tasks.OrderBy(t => t.Type).ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<IReadOnlyList<TaskStatusUpdate>> ListRecentUpdatesAsync(int limit = 500, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateAsync(cancellationToken);
        var updates = await db.StatusUpdates.AsNoTracking().ToListAsync(cancellationToken);
        return updates
            .OrderByDescending(u => u.HubReceivedAt).ThenByDescending(u => u.DeviceSeq)
            .Take(limit)
            .ToList();
    }

    public async Task<HubSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateAsync(cancellationToken);
        return await db.Settings.AsNoTracking().SingleAsync(cancellationToken);
    }
}
