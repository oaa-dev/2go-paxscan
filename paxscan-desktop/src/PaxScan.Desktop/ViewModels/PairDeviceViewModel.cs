using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PaxScan.Application.Devices;
using PaxScan.Application.Pairing;
using PaxScan.Application.Tasks;
using PaxScan.Desktop.Hosting;
using PaxScan.Desktop.Services;
using PaxScan.Infrastructure.Pairing;

namespace PaxScan.Desktop.ViewModels;

/// <summary>Shows a single-use pairing QR with a countdown and closes when a new device pairs.</summary>
public sealed partial class PairDeviceViewModel(
    CreatePairingTokenHandler createToken,
    QrPayloadFactory qrFactory,
    HubQueries queries,
    DeviceMonitorQuery monitor,
    ConnectionTester tester,
    HubRuntime runtime,
    TimeProvider time) : ObservableObject, IDisposable
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private DateTimeOffset _expiresAt;
    private int _initialDeviceCount;

    public event EventHandler? CloseRequested;

    [ObservableProperty]
    public partial BitmapImage? QrImage { get; set; }

    [ObservableProperty]
    public partial string HubUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Countdown { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConnectionMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsExpired { get; set; }

    public async Task StartAsync()
    {
        _initialDeviceCount = (await monitor.HandleAsync()).Count;
        _timer.Tick += async (_, _) => await TickAsync();
        await NewCodeAsync();
        _timer.Start();
    }

    [RelayCommand]
    private async Task NewCodeAsync()
    {
        var settings = await queries.GetSettingsAsync();
        var token = await createToken.HandleAsync();
        var payload = qrFactory.Create(token, settings.HubId, settings.AdvertisedHost, runtime.Port);

        QrImage = ToBitmap(payload.Png);
        HubUrl = payload.HubUrl;
        _expiresAt = payload.ExpiresAt;
        IsExpired = false;
        UpdateCountdown();
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        ConnectionMessage = "Testing…";
        var (ok, message) = await tester.TestAsync(HubUrl);
        ConnectionMessage = (ok ? "✔ " : "✖ ") + message;
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    public void Dispose() => _timer.Stop();

    private async Task TickAsync()
    {
        UpdateCountdown();
        if ((await monitor.HandleAsync()).Count > _initialDeviceCount)
        {
            _timer.Stop();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateCountdown()
    {
        var left = _expiresAt - time.GetUtcNow();
        IsExpired = left <= TimeSpan.Zero;
        Countdown = IsExpired ? "Code expired — create a new one." : $"Code expires in {left:m\\:ss}";
    }

    private static BitmapImage ToBitmap(byte[] png)
    {
        var image = new BitmapImage();
        using var stream = new MemoryStream(png);
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
