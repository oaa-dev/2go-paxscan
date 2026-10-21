using Microsoft.EntityFrameworkCore;
using PaxScan.Application.Abstractions;
using PaxScan.Application.Common;
using PaxScan.Application.Pairing;
using PaxScan.Application.Sync;
using PaxScan.Domain.Sync;
using PaxScan.Domain.Tasks;

namespace PaxScan.Application.Tests;

public class SyncWithWebTests
{
    private static WebTaskSnapshot SnapshotOf(params WebTaskFields[] tasks) =>
        new("2026-10-15T08:00:00Z", TaskChecksum.Compute(tasks.Select(t => (t.Id, t.Version))), tasks);

    private static WebTaskFields Fields(Guid id, TaskItemStatus status, int version, string name = "Prep breakfast") =>
        new(id, name, TaskItemType.Chef, null, status, version, TestHub.Start, TestHub.Start);

    private static async Task PushDoneAsync(TestHub hub, params Guid[] taskIds)
    {
        var token = await hub.Get<CreatePairingTokenHandler>().HandleAsync();
        var device = await hub.Get<PairDeviceHandler>().HandleAsync(new PairDeviceCommand(token.Token, "Chef", "android", "1.0.0"));
        var items = taskIds
            .Select((taskId, i) => new DeviceStatusUpdate(Guid.CreateVersion7(), taskId, TaskItemStatus.Pending, TaskItemStatus.Done, 1, i + 1, TestHub.Start))
            .ToList();
        await hub.Get<PushUpdatesHandler>().HandleAsync(device.Value!.DeviceId, items);
    }

    [Fact]
    public async Task Sync_requires_online_settings()
    {
        await using var hub = await TestHub.CreateAsync();

        var result = await hub.Get<SyncWithWebHandler>().HandleAsync();

        result.Error!.Code.ShouldBe(ErrorCodes.WebNotConfigured);
    }

    [Fact]
    public async Task Download_adds_new_tasks_and_records_the_dataset_version()
    {
        await using var hub = await TestHub.CreateAsync();
        await hub.ConfigureWebAsync();
        hub.Web.Snapshot = SnapshotOf(Fields(Guid.CreateVersion7(), TaskItemStatus.Pending, 1), Fields(Guid.CreateVersion7(), TaskItemStatus.Done, 4, "Patrol deck 5"));

        var result = await hub.Get<SyncWithWebHandler>().HandleAsync();

        result.Value!.Downloaded.ShouldBe(2);
        await using var db = await hub.DbAsync();
        (await db.Tasks.CountAsync()).ShouldBe(2);
        (await db.Settings.SingleAsync()).DatasetVersion.ShouldBe("2026-10-15T08:00:00Z");
    }

    [Fact]
    public async Task Uploads_before_downloading_and_marks_updates_acked()
    {
        await using var hub = await TestHub.CreateAsync();
        await hub.ConfigureWebAsync();
        var task = await hub.SeedTaskAsync();
        await PushDoneAsync(hub, task.Id);
        hub.Web.Snapshot = SnapshotOf(Fields(task.Id, TaskItemStatus.Done, 2));

        var result = await hub.Get<SyncWithWebHandler>().HandleAsync();

        hub.Web.Calls.ShouldBe(["push", "snapshot"]);
        hub.Web.Pushed.Single().BaseVersion.ShouldBe(1);
        result.Value!.Uploaded.ShouldBe(1);
        await using var db = await hub.DbAsync();
        var update = await db.StatusUpdates.SingleAsync();
        update.UploadStatus.ShouldBe(UploadStatus.Acked);
        update.OnlineAckedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Task_with_unsent_updates_keeps_its_local_status_after_download()
    {
        await using var hub = await TestHub.CreateAsync();
        await hub.ConfigureWebAsync();
        var answered = await hub.SeedTaskAsync("Answered");
        var unanswered = await hub.SeedTaskAsync("Unanswered");
        await PushDoneAsync(hub, answered.Id, unanswered.Id);

        // Online answers only for one update; the other stays pending.
        hub.Web.OnPush = items => items
            .Where(i => i.TaskId == answered.Id)
            .Select(i => new WebItemResult(i.Id, "accepted", null, 2))
            .ToList();
        hub.Web.Snapshot = SnapshotOf(
            Fields(answered.Id, TaskItemStatus.Done, 2, "Answered"),
            Fields(unanswered.Id, TaskItemStatus.Pending, 1, "Renamed on web"));

        var result = await hub.Get<SyncWithWebHandler>().HandleAsync();

        result.Value!.Uploaded.ShouldBe(1);
        hub.Web.Calls.ShouldBe(["push", "snapshot"]);
        await using var db = await hub.DbAsync();
        var task = await db.Tasks.SingleAsync(t => t.Id == unanswered.Id);
        task.Name.ShouldBe("Renamed on web");
        task.Status.ShouldBe(TaskItemStatus.Done);
        (await db.StatusUpdates.SingleAsync(u => u.TaskId == unanswered.Id)).UploadStatus.ShouldBe(UploadStatus.Pending);
    }

    [Fact]
    public async Task No_answer_from_online_stops_the_sync_without_downloading()
    {
        await using var hub = await TestHub.CreateAsync();
        await hub.ConfigureWebAsync();
        var task = await hub.SeedTaskAsync();
        await PushDoneAsync(hub, task.Id);
        hub.Web.OnPush = _ => [];

        var result = await hub.Get<SyncWithWebHandler>().HandleAsync();

        result.Error!.Code.ShouldBe(ErrorCodes.WebUnavailable);
        hub.Web.Calls.ShouldBe(["push"]);
        await using var db = await hub.DbAsync();
        (await db.StatusUpdates.SingleAsync()).UploadStatus.ShouldBe(UploadStatus.Pending);
    }

    [Fact]
    public async Task Online_rejection_is_kept_for_investigation()
    {
        await using var hub = await TestHub.CreateAsync();
        await hub.ConfigureWebAsync();
        var task = await hub.SeedTaskAsync();
        await PushDoneAsync(hub, task.Id);
        hub.Web.OnPush = items => items.Select(i => new WebItemResult(i.Id, "rejected", "task_not_found", null)).ToList();

        var result = await hub.Get<SyncWithWebHandler>().HandleAsync();

        result.Value!.UploadRejected.ShouldBe(1);
        await using var db = await hub.DbAsync();
        var update = await db.StatusUpdates.SingleAsync();
        update.UploadStatus.ShouldBe(UploadStatus.Rejected);
        update.UploadError.ShouldBe("task_not_found");
    }

    [Fact]
    public async Task Network_failure_leaves_updates_pending_for_the_next_sync()
    {
        await using var hub = await TestHub.CreateAsync();
        await hub.ConfigureWebAsync();
        var task = await hub.SeedTaskAsync();
        await PushDoneAsync(hub, task.Id);
        hub.Web.FailWith = new WebApiException("offline");

        var result = await hub.Get<SyncWithWebHandler>().HandleAsync();

        result.Error!.Code.ShouldBe(ErrorCodes.WebUnavailable);
        await using var db = await hub.DbAsync();
        (await db.StatusUpdates.SingleAsync()).UploadStatus.ShouldBe(UploadStatus.Pending);
    }

    [Fact]
    public async Task Checksum_mismatch_changes_nothing()
    {
        await using var hub = await TestHub.CreateAsync();
        await hub.ConfigureWebAsync();
        hub.Web.Snapshot = SnapshotOf(Fields(Guid.CreateVersion7(), TaskItemStatus.Pending, 1)) with { Checksum = "bad" };

        var result = await hub.Get<SyncWithWebHandler>().HandleAsync();

        result.Error!.Code.ShouldBe(ErrorCodes.ChecksumMismatch);
        await using var db = await hub.DbAsync();
        (await db.Tasks.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Tasks_deleted_on_online_are_removed_locally()
    {
        await using var hub = await TestHub.CreateAsync();
        await hub.ConfigureWebAsync();
        await hub.SeedTaskAsync();
        hub.Web.Snapshot = SnapshotOf();

        var result = await hub.Get<SyncWithWebHandler>().HandleAsync();

        result.Value!.Removed.ShouldBe(1);
        await using var db = await hub.DbAsync();
        (await db.Tasks.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Stored_hub_token_is_sent_unprotected_to_online()
    {
        await using var hub = await TestHub.CreateAsync();
        await hub.ConfigureWebAsync(token: "secret-token");

        await using var db = await hub.DbAsync();
        var settings = await db.Settings.SingleAsync();

        hub.Get<ISecretProtector>().Unprotect(settings.HubTokenProtected!).ShouldBe("secret-token");
    }
}
