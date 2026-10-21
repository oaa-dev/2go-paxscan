using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PaxScan.Application.Tasks;
using PaxScan.Domain.Sync;

namespace PaxScan.Desktop.ViewModels;

public sealed record UpdateRow(
    string ReceivedAt,
    string TaskName,
    string Change,
    string Device,
    long DeviceSeq,
    string Result,
    bool IsConflict,
    bool IsRejected,
    string Upload,
    string? Note);

public sealed partial class UpdatesViewModel(HubQueries queries) : ObservableObject
{
    private IReadOnlyList<UpdateRow> _all = [];

    public ObservableCollection<UpdateRow> Updates { get; } = [];

    [ObservableProperty]
    public partial bool ConflictsOnly { get; set; }

    [ObservableProperty]
    public partial string Summary { get; set; } = string.Empty;

    partial void OnConflictsOnlyChanged(bool value) => ApplyFilter();

    [RelayCommand]
    public async Task RefreshAsync()
    {
        var updates = await queries.ListRecentUpdatesAsync();
        var tasks = (await queries.ListTasksAsync()).ToDictionary(t => t.Id, t => t.Name);

        _all = updates.Select(u => new UpdateRow(
            Display.Time(u.HubReceivedAt),
            tasks.GetValueOrDefault(u.TaskId, $"(deleted) {u.TaskId.ToString()[..8]}"),
            $"{Display.Wire(u.FromStatus)} → {Display.Wire(u.ToStatus)}",
            u.DeviceId.ToString()[..8],
            u.DeviceSeq,
            Display.Wire(u.BusinessStatus),
            u.BusinessStatus == UpdateBusinessStatus.Conflict,
            u.BusinessStatus == UpdateBusinessStatus.Rejected,
            Display.Wire(u.UploadStatus),
            u.RejectReason ?? u.UploadError)).ToList();

        var pending = updates.Count(u => u.UploadStatus is UploadStatus.Pending or UploadStatus.Uploading);
        var conflicts = updates.Count(u => u.BusinessStatus == UpdateBusinessStatus.Conflict);
        Summary = $"{updates.Count} updates · {conflicts} conflicts · {pending} waiting for upload";
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Updates.Clear();
        foreach (var row in _all.Where(r => !ConflictsOnly || r.IsConflict))
        {
            Updates.Add(row);
        }
    }
}
