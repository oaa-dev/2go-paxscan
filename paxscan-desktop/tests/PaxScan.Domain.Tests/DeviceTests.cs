using PaxScan.Domain.Devices;
using PaxScan.Domain.Pairing;

namespace PaxScan.Domain.Tests;

public class DeviceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 8, 0, 0, TimeSpan.Zero);

    private static Device NewDevice() => Device.Pair(Guid.NewGuid(), "Chef tablet", "android", "1.0.0", "hash", Now);

    [Fact]
    public void Contiguous_sequence_numbers_do_not_flag_a_gap()
    {
        var device = NewDevice();

        device.RecordPushed([1, 2, 3], 3, Now);
        device.RecordPushed([4], 1, Now);

        device.LastSeq.ShouldBe(4);
        device.SeqGapDetected.ShouldBeFalse();
    }

    [Fact]
    public void Missing_sequence_numbers_flag_a_gap()
    {
        var device = NewDevice();

        device.RecordPushed([1, 2, 5], 3, Now);

        device.SeqGapDetected.ShouldBeTrue();
        device.LastSeq.ShouldBe(5);
    }

    [Fact]
    public void Device_is_online_only_within_the_heartbeat_window()
    {
        var device = NewDevice();

        device.IsOnline(Now + TimeSpan.FromSeconds(59)).ShouldBeTrue();
        device.IsOnline(Now + TimeSpan.FromSeconds(61)).ShouldBeFalse();
    }

    [Fact]
    public void Hello_clears_a_sync_request_and_records_clock_offset()
    {
        var device = NewDevice();
        device.RequestSync();

        device.Hello(Now, pendingCount: 4, deviceTime: Now.AddSeconds(-2));

        device.SyncRequested.ShouldBeFalse();
        device.PendingCount.ShouldBe(4);
        device.ClockOffsetMs.ShouldBe(2000);
    }

    [Fact]
    public void Revoked_device_cannot_be_asked_to_sync()
    {
        var device = NewDevice();
        device.Revoke();

        device.RequestSync();

        device.SyncRequested.ShouldBeFalse();
        device.IsOnline(Now).ShouldBeFalse();
    }

    [Fact]
    public void Pairing_token_is_single_use_and_expires()
    {
        var token = PairingToken.Issue("hash", Now);

        token.IsUsable(Now.AddMinutes(4)).ShouldBeTrue();
        token.IsUsable(Now.AddMinutes(5)).ShouldBeFalse();

        token.Use(Now);
        token.IsUsable(Now).ShouldBeFalse();
    }
}
