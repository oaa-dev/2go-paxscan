using CommunityToolkit.Mvvm.ComponentModel;
using PaxScan.Desktop.Hosting;

namespace PaxScan.Desktop.ViewModels;

public sealed partial class MainViewModel(
    TasksViewModel tasks,
    DevicesViewModel devices,
    UpdatesViewModel updates,
    SettingsViewModel settings,
    HubRuntime runtime) : ObservableObject
{
    public TasksViewModel Tasks { get; } = tasks;

    public DevicesViewModel Devices { get; } = devices;

    public UpdatesViewModel Updates { get; } = updates;

    public SettingsViewModel Settings { get; } = settings;

    public string HubStatus => $"Hub API listening on port {runtime.Port} · certificate {runtime.CertificateFingerprint[..16]}…";

    public async Task LoadAsync()
    {
        await Settings.LoadAsync();
        await Tasks.RefreshAsync();
        await Updates.RefreshAsync();
        Devices.StartMonitoring();
    }
}
