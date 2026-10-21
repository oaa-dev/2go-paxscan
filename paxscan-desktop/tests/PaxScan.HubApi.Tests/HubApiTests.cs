using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using PaxScan.Application;
using PaxScan.Application.Abstractions;
using PaxScan.Application.Devices;
using PaxScan.Application.Pairing;
using PaxScan.Domain.Tasks;
using PaxScan.Infrastructure;
using PaxScan.Infrastructure.Persistence;

namespace PaxScan.HubApi.Tests;

public sealed class HubApiTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 10, 15, 8, 0, 0, TimeSpan.Zero);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "paxscan-tests", Guid.NewGuid().ToString("N"));
    private readonly FakeTimeProvider _time = new(Start);
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddSingleton<TimeProvider>(_time)
            .AddHubInfrastructure(o => o.DataDirectory = _directory)
            .AddHubApplication()
            .AddHubApi();

        _app = builder.Build();
        _app.UseHubApi();
        await _app.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, recursive: true);
    }

    private async Task<string> NewPairingTokenAsync() =>
        (await _app.Services.GetRequiredService<CreatePairingTokenHandler>().HandleAsync()).Token;

    private async Task<(Guid DeviceId, string Secret)> PairAsync()
    {
        var response = await _client.PostAsJsonAsync("/v1/pair", new
        {
            pairing_token = await NewPairingTokenAsync(),
            device_name = "Chef tablet",
            platform = "android",
            app_version = "1.0.0",
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("device_id").GetGuid(), body.GetProperty("device_secret").GetString()!);
    }

    private void Authenticate(Guid deviceId, string secret) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Device", $"{deviceId}:{secret}");

    private async Task<TaskItem> SeedTaskAsync()
    {
        var task = TaskItem.FromWeb(new WebTaskFields(Guid.CreateVersion7(), "Prep breakfast", TaskItemType.Chef, null, TaskItemStatus.Pending, 1, Start, Start));
        await using var db = await _app.Services.GetRequiredService<IHubDbContextFactory>().CreateAsync();
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task;
    }

    [Fact]
    public async Task Full_device_flow_pair_ping_hello_push_snapshot()
    {
        var task = await SeedTaskAsync();
        var (deviceId, secret) = await PairAsync();
        Authenticate(deviceId, secret);

        var ping = await _client.PostAsJsonAsync("/v1/ping", new { pending_count = 1, last_seq = 0 });
        (await ping.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sync_requested").GetBoolean().ShouldBeFalse();

        var hello = await _client.PostAsJsonAsync("/v1/sync/hello", new { last_seq = 1, pending_count = 1, device_time = Start });
        (await hello.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("expected_from_seq").GetInt64().ShouldBe(1);

        var updateId = Guid.CreateVersion7();
        var push = await _client.PostAsJsonAsync("/v1/sync/push", new
        {
            items = new[]
            {
                new
                {
                    id = updateId, task_id = task.Id, from_status = "pending", to_status = "in_progress",
                    base_version = 1, device_seq = 1, device_created_at = Start,
                },
            },
        });
        push.StatusCode.ShouldBe(HttpStatusCode.OK);
        var pushBody = await push.Content.ReadFromJsonAsync<JsonElement>();
        var result = pushBody.GetProperty("results")[0];
        result.GetProperty("id").GetGuid().ShouldBe(updateId);
        result.GetProperty("result").GetString().ShouldBe("accepted");
        result.GetProperty("task_version").GetInt32().ShouldBe(2);
        pushBody.GetProperty("seq_gap_detected").GetBoolean().ShouldBeFalse();

        var snapshot = await _client.GetFromJsonAsync<JsonElement>("/v1/sync/snapshot");
        var row = snapshot.GetProperty("tasks")[0];
        row.GetProperty("status").GetString().ShouldBe("in_progress");
        row.GetProperty("type").GetString().ShouldBe("chef");
        row.GetProperty("version").GetInt32().ShouldBe(2);
        row.TryGetProperty("web_version", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Sync_request_from_desktop_reaches_device_on_next_ping()
    {
        var (deviceId, secret) = await PairAsync();
        Authenticate(deviceId, secret);
        await _app.Services.GetRequiredService<DeviceAdminHandlers>().RequestSyncAsync(null);

        var ping = await _client.PostAsJsonAsync("/v1/ping", new { pending_count = 0, last_seq = 0 });

        (await ping.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sync_requested").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Requests_without_credentials_are_unauthorized()
    {
        var response = await _client.GetAsync("/v1/sync/snapshot");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Wrong_secret_is_unauthorized()
    {
        var (deviceId, _) = await PairAsync();
        Authenticate(deviceId, "not-the-secret");

        var response = await _client.GetAsync("/v1/sync/snapshot");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoked_device_is_unauthorized()
    {
        var (deviceId, secret) = await PairAsync();
        Authenticate(deviceId, secret);
        await _app.Services.GetRequiredService<DeviceAdminHandlers>().RevokeAsync(deviceId);

        var response = await _client.PostAsJsonAsync("/v1/ping", new { pending_count = 0, last_seq = 0 });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reused_pairing_token_is_refused_with_problem_details()
    {
        var token = await NewPairingTokenAsync();
        var body = new { pairing_token = token, device_name = "A", platform = "android", app_version = "1.0.0" };
        (await _client.PostAsJsonAsync("/v1/pair", body)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var second = await _client.PostAsJsonAsync("/v1/pair", body);

        second.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        second.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString().ShouldBe("pairing_token_invalid");
    }

    [Fact]
    public async Task Unknown_platform_is_a_bad_request()
    {
        var response = await _client.PostAsJsonAsync("/v1/pair", new
        {
            pairing_token = await NewPairingTokenAsync(),
            device_name = "A",
            platform = "windows-phone",
            app_version = "1.0.0",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
