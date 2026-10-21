using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using PaxScan.Application;
using PaxScan.Desktop.Services;
using PaxScan.Desktop.ViewModels;
using PaxScan.Desktop.Views;
using PaxScan.HubApi;
using PaxScan.Infrastructure;
using PaxScan.Infrastructure.Security;
using Serilog;

namespace PaxScan.Desktop.Hosting;

/// <summary>
/// One host and one DI container for both the WPF UI and the embedded hub API, so the screens
/// and the phones go through the same use cases.
/// </summary>
internal static class HubHostBuilder
{
    public static WebApplication Build(HubOptions options)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory });

        builder.Host.UseSerilog((_, logger) => logger
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.File(
                Path.Combine(options.DataDirectory, "logs", "hub-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                formatProvider: CultureInfo.InvariantCulture));

        builder.Services
            .AddHubInfrastructure(o =>
            {
                o.DataDirectory = options.DataDirectory;
                o.ConnectionString = options.ConnectionString;
            })
            .AddHubApplication()
            .AddHubApi();

        builder.Services.AddSingleton<HubRuntime>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<ConnectionTester>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<TasksViewModel>();
        builder.Services.AddSingleton<DevicesViewModel>();
        builder.Services.AddSingleton<UpdatesViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddTransient<PairDeviceViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        // The port comes from the database, which is only ready after Build(); App binds the URL
        // (app.Urls) before StartAsync. The certificate is resolved lazily on the first handshake.
        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.Limits.MaxRequestBodySize = 2 * 1024 * 1024;
            kestrel.ConfigureHttpsDefaults(https =>
            {
                var certificates = kestrel.ApplicationServices.GetRequiredService<CertificateService>();
                https.ServerCertificateSelector = (_, _) => certificates.GetOrCreate();
            });
        });

        var app = builder.Build();
        app.UseHubApi();
        return app;
    }
}
