using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using PaxScan.Application.Common;

namespace PaxScan.HubApi;

public static class HubApiExtensions
{
    public const string PairRateLimitPolicy = "pair";

    public static IServiceCollection AddHubApi(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.ConfigureHttpJsonOptions(o => WireJson.Configure(o.SerializerOptions));

        services.AddAuthentication(DeviceAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DeviceAuthenticationHandler>(DeviceAuthenticationHandler.SchemeName, null);
        services.AddAuthorization();

        // Pairing tokens are random 256-bit values; the limit just stops a noisy device looping.
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddFixedWindowLimiter(PairRateLimitPolicy, w =>
            {
                w.PermitLimit = 10;
                w.Window = TimeSpan.FromMinutes(1);
                w.QueueLimit = 0;
            });
        });

        return services;
    }

    public static WebApplication UseHubApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHubEndpoints();
        return app;
    }
}
