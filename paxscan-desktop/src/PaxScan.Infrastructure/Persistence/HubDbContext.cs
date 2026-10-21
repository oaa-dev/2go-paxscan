using Microsoft.EntityFrameworkCore;
using PaxScan.Application.Abstractions;
using PaxScan.Domain.Devices;
using PaxScan.Domain.Pairing;
using PaxScan.Domain.Settings;
using PaxScan.Domain.Sync;
using PaxScan.Domain.Tasks;

namespace PaxScan.Infrastructure.Persistence;

public sealed class HubDbContext(DbContextOptions<HubDbContext> options) : DbContext(options), IHubDbContext
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<TaskStatusUpdate> StatusUpdates => Set<TaskStatusUpdate>();

    public DbSet<Device> Devices => Set<Device>();

    public DbSet<PairingToken> PairingTokens => Set<PairingToken>();

    public DbSet<HubSettings> Settings => Set<HubSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HubDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksConverter>();
        configurationBuilder.Properties<TaskItemType>().HaveConversion<SnakeCaseEnumConverter<TaskItemType>>().HaveMaxLength(20);
        configurationBuilder.Properties<TaskItemStatus>().HaveConversion<SnakeCaseEnumConverter<TaskItemStatus>>().HaveMaxLength(20);
        configurationBuilder.Properties<UpdateBusinessStatus>().HaveConversion<SnakeCaseEnumConverter<UpdateBusinessStatus>>().HaveMaxLength(20);
        configurationBuilder.Properties<UploadStatus>().HaveConversion<SnakeCaseEnumConverter<UploadStatus>>().HaveMaxLength(20);
        configurationBuilder.Properties<DeviceStatus>().HaveConversion<SnakeCaseEnumConverter<DeviceStatus>>().HaveMaxLength(20);
    }
}
