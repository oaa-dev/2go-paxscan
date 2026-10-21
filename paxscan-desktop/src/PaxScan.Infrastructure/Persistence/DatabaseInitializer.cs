using Microsoft.EntityFrameworkCore;
using PaxScan.Domain.Settings;

namespace PaxScan.Infrastructure.Persistence;

/// <summary>Applies migrations and creates the hub's identity on first run.</summary>
public sealed class DatabaseInitializer(IDbContextFactory<HubDbContext> factory, TimeProvider time)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);

        if (!await db.Settings.AnyAsync(cancellationToken))
        {
            db.Settings.Add(HubSettings.CreateDefault(Guid.CreateVersion7(time.GetUtcNow())));
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
