// Headless PaxScan hub for development (no WPF). Usage:
//   dotnet run --project tools/PaxScan.HubDevHost -- [--data <dir>] [--port 5443] [--host <lan-ip>]
//       [--web <online-url> --token <hub-token> --sync] [--pair] [--request-sync]
using Microsoft.Extensions.DependencyInjection;
using PaxScan.Application;
using PaxScan.Application.Devices;
using PaxScan.Application.Pairing;
using PaxScan.Application.Settings;
using PaxScan.Application.Sync;
using PaxScan.Application.Tasks;
using PaxScan.HubApi;
using PaxScan.Infrastructure;
using PaxScan.Infrastructure.Pairing;
using PaxScan.Infrastructure.Persistence;
using PaxScan.Infrastructure.Security;
using QRCoder;

var arguments = Args.Parse(args);
var dataDirectory = arguments.Get("data") ?? Path.Combine(Path.GetTempPath(), "paxscan-devhub");
Directory.CreateDirectory(dataDirectory);

var builder = WebApplication.CreateBuilder();
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Services.AddHubInfrastructure(o => o.DataDirectory = dataDirectory).AddHubApplication().AddHubApi();

var port = int.Parse(arguments.Get("port") ?? "5443", System.Globalization.CultureInfo.InvariantCulture);
builder.WebHost.ConfigureKestrel(k =>
    k.ListenAnyIP(port, l => l.UseHttps(k.ApplicationServices.GetRequiredService<CertificateService>().GetOrCreate())));

var app = builder.Build();
app.UseHubApi();

var services = app.Services;
await services.GetRequiredService<DatabaseInitializer>().InitializeAsync();
var settings = await services.GetRequiredService<HubQueries>().GetSettingsAsync();

if (arguments.Get("web") is { } web)
{
    var saved = await services.GetRequiredService<UpdateSettingsHandler>()
        .HandleAsync(new UpdateSettingsCommand(web, arguments.Get("token"), arguments.Get("host") ?? settings.AdvertisedHost, port));
    Console.WriteLine(saved.IsSuccess ? $"Online set to {web}" : $"Settings error: {saved.Error!.Message}");
}

if (arguments.Has("sync"))
{
    var result = await services.GetRequiredService<SyncWithWebHandler>().HandleAsync();
    Console.WriteLine(result.IsSuccess
        ? $"Synced with Online: {result.Value}"
        : $"Sync with Online failed: {result.Error!.Message}");
}

if (arguments.Has("request-sync"))
{
    var count = await services.GetRequiredService<DeviceAdminHandlers>().RequestSyncAsync(null);
    Console.WriteLine($"Sync requested on {count} device(s).");
}

await app.StartAsync();
Console.WriteLine($"Hub {settings.HubId} listening on https://0.0.0.0:{port} (data: {dataDirectory})");
Console.WriteLine($"Certificate SHA-256: {services.GetRequiredService<CertificateService>().GetFingerprint()}");

if (arguments.Has("pair"))
{
    var token = await services.GetRequiredService<CreatePairingTokenHandler>().HandleAsync();
    var qr = services.GetRequiredService<QrPayloadFactory>().Create(token, settings.HubId, arguments.Get("host") ?? settings.AdvertisedHost, port);
    using var data = QRCodeGenerator.GenerateQrCode(qr.Json, QRCodeGenerator.ECCLevel.M);
    Console.WriteLine(new AsciiQRCode(data).GetGraphicSmall());
    Console.WriteLine($"Pairing payload (valid until {qr.ExpiresAt:HH:mm:ss} UTC):\n{qr.Json}");
}

await app.WaitForShutdownAsync();

internal sealed class Args(Dictionary<string, string?> values)
{
    public static Args Parse(string[] args)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = args[i][2..];
            var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
            values[key] = hasValue ? args[++i] : null;
        }

        return new Args(values);
    }

    public string? Get(string key) => values.GetValueOrDefault(key);

    public bool Has(string key) => values.ContainsKey(key);
}
