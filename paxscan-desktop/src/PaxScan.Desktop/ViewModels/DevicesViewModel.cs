using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PaxScan.Application.Devices;
using PaxScan.Desktop.Services;
using PaxScan.Domain.Devices;

namespace PaxScan.Desktop.ViewModels;

public sealed record DeviceRowVm(
    Guid Id,
    string Name,
    string Platform,
    string AppVersion,
    bool IsOnline,
    bool IsRevoked,
    string State,
    string LastSeen,
    string LastSync,
    int PendingCount,
    long LastSeq,
    bool SeqGapDetected,
    bool SyncRequested);

/// <summary>Live device monitor; refreshes every 5 seconds.</summary>
public sealed partial class DevicesViewModel(
    DeviceMonitorQuery monitor,
    DeviceAdminHandlers admin,
    IDialogService dialogs,
    UpdatesViewModel updates,
    TasksViewModel tasks,
    TimeProvider time) : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private bool _monitoring;

    public ObservableCollection<DeviceRowVm> Devices { get; } = [];

    [ObservableProperty]
    public partial string Summary { get; set; } = string.Empty;

    public void StartMonitoring()
    {
        if (_monitoring)
        {
            return;
        }

        _monitoring = true;
        _ = MonitorAsync(_stop.Token);
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        var now = time.GetUtcNow();
        var rows = await monitor.HandleAsync();
        Devices.Clear();
        foreach (var d in rows)
        {
            var revoked = d.Status == DeviceStatus.Revoked;
            Devices.Add(new DeviceRowVm(
                d.Id, d.Name, d.Platform, d.AppVersion, d.IsOnline, revoked,
                revoked ? "Revoked" : d.IsOnline ? "Online" : "Offline",
                Display.Ago(d.LastSeenAt, now), Display.Ago(d.LastSyncAt, now),
                d.PendingCount, d.LastSeq, d.SeqGapDetected, d.SyncRequested));
        }

        var active = rows.Where(r => r.Status == DeviceStatus.Active).ToList();
        Summary = $"{active.Count(r => r.IsOnline)} online · {active.Count} paired · {active.Sum(r => r.PendingCount)} updates waiting on devices";
    }

    [RelayCommand]
    private void PairNewDevice()
    {
        dialogs.ShowPairDevice();
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RequestSyncAllAsync()
    {
        await admin.RequestSyncAsync(null);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RequestSyncAsync(DeviceRowVm? device)
    {
        if (device is null)
        {
            return;
        }

        await admin.RequestSyncAsync(device.Id);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RevokeAsync(DeviceRowVm? device)
    {
        if (device is null || device.IsRevoked)
        {
            return;
        }

        var warning = device.PendingCount > 0
            ? $"\n\n{device.PendingCount} updates on this device have not been synced and will be lost."
            : string.Empty;
        if (!dialogs.Confirm("Revoke device", $"Revoke \"{device.Name}\"? It will have to pair again.{warning}"))
        {
            return;
        }

        await admin.RevokeAsync(device.Id);
        await RefreshAsync();
    }

    public void Dispose()
    {
        _stop.Cancel();
        _stop.Dispose();
    }

    private async Task MonitorAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        var lastSeqTotal = -1L;
        do
        {
            try
            {
                await RefreshAsync();

                // New data from a device → refresh the other screens too.
                var seqTotal = Devices.Sum(d => d.LastSeq);
                if (seqTotal != lastSeqTotal)
                {
                    lastSeqTotal = seqTotal;
                    await updates.RefreshAsync();
                    await tasks.RefreshAsync();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Summary = $"Monitor error: {ex.Message}";
            }
        }
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(true));
    }
}
