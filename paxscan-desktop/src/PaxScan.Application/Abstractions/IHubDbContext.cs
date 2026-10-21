using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PaxScan.Domain.Devices;
using PaxScan.Domain.Pairing;
using PaxScan.Domain.Settings;
using PaxScan.Domain.Sync;
using PaxScan.Domain.Tasks;

namespace PaxScan.Application.Abstractions;

public interface IHubDbContext : IDisposable, IAsyncDisposable
{
    DbSet<TaskItem> Tasks { get; }

    DbSet<TaskStatusUpdate> StatusUpdates { get; }

    DbSet<Device> Devices { get; }

    DbSet<PairingToken> PairingTokens { get; }

    DbSet<HubSettings> Settings { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
