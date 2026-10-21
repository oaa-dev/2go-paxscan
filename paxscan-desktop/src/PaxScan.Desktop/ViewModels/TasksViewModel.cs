using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PaxScan.Application.Sync;
using PaxScan.Application.Tasks;
using PaxScan.Domain.Tasks;

namespace PaxScan.Desktop.ViewModels;

public sealed record TaskRow(string Name, string Type, string Status, int Version, int WebVersion, string? Description, string UpdatedAt);

public sealed partial class TasksViewModel(HubQueries queries, SyncWithWebHandler syncWithWeb, UpdatesViewModel updates) : ObservableObject
{
    public ObservableCollection<TaskRow> Tasks { get; } = [];

    [ObservableProperty]
    public partial string SyncMessage { get; set; } = "Not synced with Online yet.";

    [ObservableProperty]
    public partial bool SyncFailed { get; set; }

    public async Task RefreshAsync()
    {
        var tasks = await queries.ListTasksAsync();
        Tasks.Clear();
        foreach (var t in tasks)
        {
            Tasks.Add(new TaskRow(t.Name, Display.Wire(t.Type), Display.Wire(t.Status), t.Version, t.WebVersion, t.Description, Display.Time(t.UpdatedAt)));
        }

        var settings = await queries.GetSettingsAsync();
        if (settings.LastWebSyncAt is { } last && !SyncFailed)
        {
            SyncMessage = $"Last synced with Online {Display.Time(last)}.";
        }
    }

    [RelayCommand]
    private async Task SyncWithWebAsync(CancellationToken cancellationToken)
    {
        SyncMessage = "Syncing with Online…";
        var result = await syncWithWeb.HandleAsync(cancellationToken);
        SyncFailed = !result.IsSuccess;
        SyncMessage = result.IsSuccess
            ? $"Uploaded {result.Value!.Uploaded} (conflicts {result.Value.Conflicts}, rejected {result.Value.UploadRejected}) · downloaded {result.Value.Downloaded} tasks · removed {result.Value.Removed}."
            : $"Sync failed: {result.Error!.Message}";

        await RefreshAsync();
        await updates.RefreshAsync();
    }
}
