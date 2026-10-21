using PaxScan.Domain.Sync;
using PaxScan.Domain.Tasks;

namespace PaxScan.Domain.Tests;

public class TaskItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 8, 0, 0, TimeSpan.Zero);

    private static TaskItem NewTask(TaskItemStatus status = TaskItemStatus.Pending, int version = 3) =>
        TaskItem.FromWeb(new WebTaskFields(Guid.NewGuid(), "Prep breakfast", TaskItemType.Chef, null, status, version, Now, Now));

    [Fact]
    public void Update_based_on_current_version_is_accepted_and_bumps_version()
    {
        var task = NewTask();

        var outcome = task.ApplyStatusUpdate(TaskItemStatus.Pending, TaskItemStatus.InProgress, baseVersion: 3, Now);

        outcome.BusinessStatus.ShouldBe(UpdateBusinessStatus.Accepted);
        outcome.PreviousVersion.ShouldBe(3);
        outcome.ResultingVersion.ShouldBe(4);
        task.Status.ShouldBe(TaskItemStatus.InProgress);
        task.Version.ShouldBe(4);
    }

    [Fact]
    public void Update_based_on_stale_version_is_applied_but_flagged_as_conflict()
    {
        var task = NewTask();
        task.ApplyStatusUpdate(TaskItemStatus.Pending, TaskItemStatus.InProgress, 3, Now);

        var outcome = task.ApplyStatusUpdate(TaskItemStatus.Pending, TaskItemStatus.Done, baseVersion: 3, Now);

        outcome.BusinessStatus.ShouldBe(UpdateBusinessStatus.Conflict);
        task.Status.ShouldBe(TaskItemStatus.Done);
        task.Version.ShouldBe(5);
    }

    [Fact]
    public void Update_to_the_same_status_is_rejected_without_changing_the_task()
    {
        var task = NewTask();

        var outcome = task.ApplyStatusUpdate(TaskItemStatus.Pending, TaskItemStatus.Pending, 3, Now);

        outcome.BusinessStatus.ShouldBe(UpdateBusinessStatus.Rejected);
        outcome.RejectReason.ShouldBe(StatusUpdateRejectReason.InvalidTransition);
        task.Version.ShouldBe(3);
    }

    [Fact]
    public void Web_refresh_keeps_local_status_while_updates_wait_for_upload()
    {
        var task = NewTask();
        task.ApplyStatusUpdate(TaskItemStatus.Pending, TaskItemStatus.Done, 3, Now);

        task.ReplaceFromWeb(
            new WebTaskFields(task.Id, "Renamed", TaskItemType.Chef, "desc", TaskItemStatus.Pending, 7, Now, Now),
            keepLocalStatus: true);

        task.Name.ShouldBe("Renamed");
        task.WebVersion.ShouldBe(7);
        task.Status.ShouldBe(TaskItemStatus.Done);
        task.Version.ShouldBe(4);
    }

    [Fact]
    public void Web_refresh_without_pending_updates_takes_online_status_and_version()
    {
        var task = NewTask();

        task.ReplaceFromWeb(
            new WebTaskFields(task.Id, "Prep breakfast", TaskItemType.Chef, null, TaskItemStatus.Done, 9, Now, Now),
            keepLocalStatus: false);

        task.Status.ShouldBe(TaskItemStatus.Done);
        task.Version.ShouldBe(9);
    }
}
