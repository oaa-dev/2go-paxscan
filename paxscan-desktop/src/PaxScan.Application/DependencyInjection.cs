using Microsoft.Extensions.DependencyInjection;
using PaxScan.Application.Devices;
using PaxScan.Application.Pairing;
using PaxScan.Application.Settings;
using PaxScan.Application.Sync;
using PaxScan.Application.Tasks;

namespace PaxScan.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddHubApplication(this IServiceCollection services)
    {
        services.AddSingleton<CreatePairingTokenHandler>();
        services.AddSingleton<PairDeviceHandler>();
        services.AddSingleton<DeviceAuthenticator>();
        services.AddSingleton<RecordHeartbeatHandler>();
        services.AddSingleton<DeviceAdminHandlers>();
        services.AddSingleton<DeviceMonitorQuery>();
        services.AddSingleton<HelloHandler>();
        services.AddSingleton<PushUpdatesHandler>();
        services.AddSingleton<BuildSnapshotHandler>();
        services.AddSingleton<SyncWithWebHandler>();
        services.AddSingleton<HubQueries>();
        services.AddSingleton<UpdateSettingsHandler>();
        return services;
    }
}
