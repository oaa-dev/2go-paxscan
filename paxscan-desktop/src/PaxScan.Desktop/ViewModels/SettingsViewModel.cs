using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PaxScan.Application.Settings;
using PaxScan.Application.Tasks;
using PaxScan.Desktop.Hosting;

namespace PaxScan.Desktop.ViewModels;

public sealed partial class SettingsViewModel(HubQueries queries, UpdateSettingsHandler update, HubRuntime runtime) : ObservableObject
{
    private int _loadedPort;

    public ObservableCollection<string> LanAddresses { get; } = [];

    [ObservableProperty]
    public partial string WebBaseUrl { get; set; } = string.Empty;

    /// <summary>Set from the PasswordBox; empty keeps the stored token.</summary>
    public string NewHubToken { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasHubToken { get; set; }

    [ObservableProperty]
    public partial string? AdvertisedHost { get; set; }

    [ObservableProperty]
    public partial int Port { get; set; }

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    public string HubId { get; private set; } = string.Empty;

    public string CertificateFingerprint => runtime.CertificateFingerprint;

    public string FirewallCommand =>
        $"netsh advfirewall firewall add rule name=\"PaxScan Hub\" dir=in action=allow protocol=TCP localport={Port} profile=private";

    partial void OnPortChanged(int value) => OnPropertyChanged(nameof(FirewallCommand));

    public async Task LoadAsync()
    {
        var settings = await queries.GetSettingsAsync();
        WebBaseUrl = settings.WebBaseUrl ?? string.Empty;
        HasHubToken = settings.HubTokenProtected is not null;
        Port = _loadedPort = settings.Port;
        HubId = settings.HubId.ToString();
        OnPropertyChanged(nameof(HubId));

        LanAddresses.Clear();
        foreach (var address in Infrastructure.Networking.LanAddresses.GetIPv4Addresses())
        {
            LanAddresses.Add(address);
        }

        AdvertisedHost = settings.AdvertisedHost ?? LanAddresses.FirstOrDefault();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var result = await update.HandleAsync(new UpdateSettingsCommand(WebBaseUrl, NewHubToken, AdvertisedHost, Port));
        if (!result.IsSuccess)
        {
            Message = result.Error!.Message;
            return;
        }

        HasHubToken = HasHubToken || !string.IsNullOrWhiteSpace(NewHubToken);
        Message = Port != _loadedPort
            ? "Saved. Restart PaxScan to listen on the new port (phones must pair again)."
            : "Saved.";
    }

    [RelayCommand]
    private void CopyFirewallCommand()
    {
        Clipboard.SetText(FirewallCommand);
        Message = "Firewall command copied. Paste it into an elevated (Administrator) terminal.";
    }
}
