using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaxScan.Domain.Devices;
using PaxScan.Domain.Pairing;
using PaxScan.Domain.Settings;
using PaxScan.Domain.Sync;
using PaxScan.Domain.Tasks;

namespace PaxScan.Infrastructure.Persistence.Configurations;

internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("tasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Name).HasMaxLength(150).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(2000);
    }
}

internal sealed class TaskStatusUpdateConfiguration : IEntityTypeConfiguration<TaskStatusUpdate>
{
    public void Configure(EntityTypeBuilder<TaskStatusUpdate> builder)
    {
        // No FK to tasks: an update for an unknown task is still stored (rejected task_not_found).
        builder.ToTable("task_status_updates");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.RejectReason).HasMaxLength(100);
        builder.Property(u => u.UploadError).HasMaxLength(500);
        builder.HasIndex(u => new { u.DeviceId, u.DeviceSeq }).IsUnique();
        builder.HasIndex(u => new { u.UploadStatus, u.HubReceivedAt });
        builder.HasIndex(u => u.TaskId);
    }
}

internal sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("devices");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Platform).HasMaxLength(20).IsRequired();
        builder.Property(d => d.AppVersion).HasMaxLength(50).IsRequired();
        builder.Property(d => d.SecretHash).HasMaxLength(128).IsRequired();
        builder.Ignore(d => d.IsActive);
    }
}

internal sealed class PairingTokenConfiguration : IEntityTypeConfiguration<PairingToken>
{
    public void Configure(EntityTypeBuilder<PairingToken> builder)
    {
        builder.ToTable("pairing_tokens");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(p => p.TokenHash).IsUnique();
    }
}

internal sealed class HubSettingsConfiguration : IEntityTypeConfiguration<HubSettings>
{
    public void Configure(EntityTypeBuilder<HubSettings> builder)
    {
        builder.ToTable("hub_settings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.WebBaseUrl).HasMaxLength(500);
        builder.Property(s => s.AdvertisedHost).HasMaxLength(255);
        builder.Property(s => s.DatasetVersion).HasMaxLength(64);
    }
}
