using System.IO;
using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaxScan.Application.Tasks;
using PaxScan.Desktop.Hosting;
using PaxScan.Desktop.Views;
using PaxScan.Infrastructure;
using PaxScan.Infrastructure.Persistence;
using PaxScan.Infrastructure.Security;

namespace PaxScan.Desktop;

public partial class App : System.Windows.Application
{
    private WebApplication? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var options = new HubOptions();
            Directory.CreateDirectory(options.DataDirectory);
            _host = HubHostBuilder.Build(options);

            var services = _host.Services;
            await services.GetRequiredService<DatabaseInitializer>().InitializeAsync();

            var settings = await services.GetRequiredService<HubQueries>().GetSettingsAsync();
            var runtime = services.GetRequiredService<HubRuntime>();
            runtime.Port = settings.Port;
            runtime.CertificateFingerprint = services.GetRequiredService<CertificateService>().GetFingerprint();

            _host.Urls.Clear();
            _host.Urls.Add($"https://0.0.0.0:{runtime.Port}");
            await _host.StartAsync();

            var window = services.GetRequiredService<MainWindow>();
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"PaxScan could not start.\n\n{ex.Message}\n\nIs another copy running, or is the hub port in use?",
                "PaxScan",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            await _host.DisposeAsync();
        }

        base.OnExit(e);
    }
}
