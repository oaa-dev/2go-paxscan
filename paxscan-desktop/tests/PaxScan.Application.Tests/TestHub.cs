using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using PaxScan.Application.Abstractions;
using PaxScan.Domain.Tasks;
using PaxScan.Infrastructure;
using PaxScan.Infrastructure.Persistence;

namespace PaxScan.Application.Tests;

/// <summary>Real Application + Infrastructure on a temp SQLite file, fake clock and fake Online.</summary>
internal sealed class TestHub : IAsyncDisposable
{
    public static readonly DateTimeOffset Start = new(2026, 10, 15, 8, 0, 0, TimeSpan.Zero);

    private readonly string _directory;

    private TestHub(ServiceProvider services, string directory, FakeTimeProvider time, FakeWebApiClient web)
    {
        Services = services;
        _directory = directory;
        Time = time;
        Web = web;
    }

    public ServiceProvider Services { get; }

    public FakeTimeProvider Time { get; }

    public FakeWebApiClient Web { get; }

    public T Get<T>()
        where T : notnull => Services.GetRequiredService<T>();

    public static async Task<TestHub> CreateAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), "paxscan-tests", Guid.NewGuid().ToString("N"));
        var time = new FakeTimeProvider(Start);
        var web = new FakeWebApiClient();

        var services = new ServiceCollection()
            .AddSingleton<TimeProvider>(time)
            .AddLogging()
            .AddHubInfrastructure(o => o.DataDirectory = directory)
            .AddHubApplication()
            .AddSingleton<IWebApiClient>(web)
            .BuildServiceProvider();

        Directory.CreateDirectory(directory);
        await services.GetRequiredService<DatabaseInitializer>().InitializeAsync();
        return new TestHub(services, directory, time, web);
    }

    public async Task<TaskItem> SeedTaskAsync(string name = "Prep breakfast", TaskItemStatus status = TaskItemStatus.Pending, int version = 1)
    {
        var task = TaskItem.FromWeb(new WebTaskFields(Guid.CreateVersion7(), name, TaskItemType.Chef, null, status, version, Start, Start));
        await using var db = await Get<IHubDbContextFactory>().CreateAsync();
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task;
    }

    public async Task ConfigureWebAsync(string url = "http://online.test", string token = "hub-token")
    {
        await using var db = await Get<IHubDbContextFactory>().CreateAsync();
        var settings = await db.Settings.SingleAsync();
        settings.UpdateConnection(url, Get<ISecretProtector>().Protect(token), null, 5443);
        await db.SaveChangesAsync();
    }

    public async Task<IHubDbContext> DbAsync() => await Get<IHubDbContextFactory>().CreateAsync();

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort; temp folder.
        }
    }
}

internal sealed class FakeWebApiClient : IWebApiClient
{
    public List<string> Calls { get; } = [];

    public List<WebStatusUpdate> Pushed { get; } = [];

    public WebTaskSnapshot Snapshot { get; set; } = new(null, Common.TaskChecksum.Compute([]), []);

    /// <summary>Default: Online accepts everything.</summary>
    public Func<IReadOnlyList<WebStatusUpdate>, IReadOnlyList<WebItemResult>> OnPush { get; set; } =
        items => items.Select(i => new WebItemResult(i.Id, "accepted", null, i.BaseVersion + 1)).ToList();

    public Exception? FailWith { get; set; }

    public Task<WebTaskSnapshot> GetTaskSnapshotAsync(WebConnection connection, CancellationToken cancellationToken = default)
    {
        Calls.Add("snapshot");
        return FailWith is null ? Task.FromResult(Snapshot) : Task.FromException<WebTaskSnapshot>(FailWith);
    }

    public Task<IReadOnlyList<WebItemResult>> PushTaskUpdatesAsync(
        WebConnection connection,
        Guid hubId,
        IReadOnlyList<WebStatusUpdate> items,
        CancellationToken cancellationToken = default)
    {
        Calls.Add("push");
        if (FailWith is not null)
        {
            return Task.FromException<IReadOnlyList<WebItemResult>>(FailWith);
        }

        Pushed.AddRange(items);
        return Task.FromResult(OnPush(items));
    }
}
