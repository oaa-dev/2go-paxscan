using Microsoft.EntityFrameworkCore;
using PaxScan.Application.Pairing;
using PaxScan.Application.Sync;
using PaxScan.Domain.Sync;
using PaxScan.Domain.Tasks;

namespace PaxScan.Application.Tests;

public class PushUpdatesTests
{
    private static async Task<Guid> PairAsync(TestHub hub)
    {
        var token = await hub.Get<CreatePairingTokenHandler>().HandleAsync();
        var result = await hub.Get<PairDeviceHandler>().HandleAsync(new PairDeviceCommand(token.Token, "Chef", "android", "1.0.0"));
        return result.Value!.DeviceId;
    }

    private static DeviceStatusUpdate Update(Guid taskId, long seq, TaskItemStatus from, TaskItemStatus to, int baseVersion) =>
        new(Guid.CreateVersion7(), taskId, from, to, baseVersion, seq, TestHub.Start.AddHours(-3));

    [Fact]
    public async Task Accepted_update_changes_the_task_and_is_stamped_with_hub_time()
    {
        await using var hub = await TestHub.CreateAsync();
        var deviceId = await PairAsync(hub);
        var task = await hub.SeedTaskAsync();
        hub.Time.Advance(TimeSpan.FromMinutes(10));

        var result = await hub.Get<PushUpdatesHandler>().HandleAsync(deviceId,
            [Update(task.Id, 1, TaskItemStatus.Pending, TaskItemStatus.InProgress, 1)]);

        result.Value!.Results.Single().Result.ShouldBe(ItemResultCodes.Accepted);
        result.Value.Results.Single().TaskVersion.ShouldBe(2);
        await using var db = await hub.DbAsync();
        (await db.Tasks.SingleAsync()).Status.ShouldBe(TaskItemStatus.InProgress);
        var stored = await db.StatusUpdates.SingleAsync();
        stored.HubReceivedAt.ShouldBe(TestHub.Start.AddMinutes(10));
        stored.UploadStatus.ShouldBe(UploadStatus.Pending);
        stored.WebBaseVersion.ShouldBe(1);
    }

    [Fact]
    public async Task Resending_the_same_batch_is_idempotent()
    {
        await using var hub = await TestHub.CreateAsync();
        var deviceId = await PairAsync(hub);
        var task = await hub.SeedTaskAsync();
        DeviceStatusUpdate[] batch =
        [
            Update(task.Id, 1, TaskItemStatus.Pending, TaskItemStatus.InProgress, 1),
            Update(task.Id, 2, TaskItemStatus.InProgress, TaskItemStatus.Done, 2),
        ];
        var handler = hub.Get<PushUpdatesHandler>();
        await handler.HandleAsync(deviceId, batch);

        var again = await handler.HandleAsync(deviceId, batch);

        again.Value!.Results.Select(r => r.Result).ShouldAllBe(r => r == ItemResultCodes.AlreadyReceived);
        again.Value.Results.Select(r => r.TaskVersion).ShouldBe([2, 3]);
        await using var db = await hub.DbAsync();
        (await db.StatusUpdates.CountAsync()).ShouldBe(2);
        (await db.Tasks.SingleAsync()).Version.ShouldBe(3);
    }

    [Fact]
    public async Task Two_devices_changing_the_same_task_produce_a_conflict_and_last_received_wins()
    {
        await using var hub = await TestHub.CreateAsync();
        var deviceA = await PairAsync(hub);
        var deviceB = await PairAsync(hub);
        var task = await hub.SeedTaskAsync();
        var handler = hub.Get<PushUpdatesHandler>();

        await handler.HandleAsync(deviceA, [Update(task.Id, 1, TaskItemStatus.Pending, TaskItemStatus.Done, 1)]);
        var second = await handler.HandleAsync(deviceB, [Update(task.Id, 1, TaskItemStatus.Pending, TaskItemStatus.InProgress, 1)]);

        second.Value!.Results.Single().Result.ShouldBe(ItemResultCodes.Conflict);
        await using var db = await hub.DbAsync();
        (await db.Tasks.SingleAsync()).Status.ShouldBe(TaskItemStatus.InProgress);
    }

    [Fact]
    public async Task Update_for_unknown_task_is_stored_as_rejected_and_never_uploaded()
    {
        await using var hub = await TestHub.CreateAsync();
        var deviceId = await PairAsync(hub);

        var result = await hub.Get<PushUpdatesHandler>().HandleAsync(deviceId,
            [Update(Guid.NewGuid(), 1, TaskItemStatus.Pending, TaskItemStatus.Done, 1)]);

        result.Value!.Results.Single().Result.ShouldBe(ItemResultCodes.Rejected);
        result.Value.Results.Single().Reason.ShouldBe(StatusUpdateRejectReason.TaskNotFound);
        await using var db = await hub.DbAsync();
        (await db.StatusUpdates.SingleAsync()).UploadStatus.ShouldBe(UploadStatus.Skipped);
    }

    [Fact]
    public async Task Gap_in_device_sequence_is_flagged()
    {
        await using var hub = await TestHub.CreateAsync();
        var deviceId = await PairAsync(hub);
        var task = await hub.SeedTaskAsync();

        var result = await hub.Get<PushUpdatesHandler>().HandleAsync(deviceId,
            [Update(task.Id, 3, TaskItemStatus.Pending, TaskItemStatus.Done, 1)]);

        result.Value!.SeqGapDetected.ShouldBeTrue();
    }

    [Fact]
    public async Task Hello_returns_the_next_expected_sequence()
    {
        await using var hub = await TestHub.CreateAsync();
        var deviceId = await PairAsync(hub);
        var task = await hub.SeedTaskAsync();
        await hub.Get<PushUpdatesHandler>().HandleAsync(deviceId,
            [Update(task.Id, 1, TaskItemStatus.Pending, TaskItemStatus.Done, 1)]);

        var hello = await hub.Get<HelloHandler>().HandleAsync(new HelloCommand(deviceId, 1, 0, TestHub.Start));

        hello.Value!.ExpectedFromSeq.ShouldBe(2);
    }
}
