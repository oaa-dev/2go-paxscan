using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using PaxScan.Application.Abstractions;
using PaxScan.Infrastructure.Pairing;
using PaxScan.Infrastructure.Persistence;
using PaxScan.Infrastructure.Security;
using PaxScan.Infrastructure.Web;

namespace PaxScan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHubInfrastructure(this IServiceCollection services, Action<HubOptions>? configure = null)
    {
        var optionsBuilder = services.AddOptions<HubOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<SqlitePragmaInterceptor>();
        services.AddDbContextFactory<HubDbContext>((sp, db) => db
            .UseSqlite(sp.GetRequiredService<IOptions<HubOptions>>().Value.ResolveConnectionString())
            .AddInterceptors(sp.GetRequiredService<SqlitePragmaInterceptor>()));
        services.AddSingleton<IHubDbContextFactory, HubDbContextFactory>();
        services.AddSingleton<IWriteGate, WriteGate>();
        services.AddSingleton<DatabaseInitializer>();

        services.AddSingleton<ISecretService, SecretService>();
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<ISecretProtector, DpapiSecretProtector>();
        }
        else
        {
            services.AddSingleton<ISecretProtector, DevelopmentSecretProtector>();
        }

        services.AddSingleton<CertificateService>();
        services.AddSingleton<QrPayloadFactory>();

        // Retrying POSTs is safe: Online's task-updates endpoint is idempotent by update id.
        services.AddHttpClient<IWebApiClient, WebApiClient>()
            .AddStandardResilienceHandler();

        return services;
    }
}
