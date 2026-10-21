using Microsoft.EntityFrameworkCore;
using PaxScan.Application.Abstractions;
using PaxScan.Application.Common;
using PaxScan.Application.Pairing;

namespace PaxScan.Application.Tests;

public class PairingTests
{
    [Fact]
    public async Task Pairing_creates_an_active_device_and_stores_only_the_secret_hash()
    {
        await using var hub = await TestHub.CreateAsync();
        var token = await hub.Get<CreatePairingTokenHandler>().HandleAsync();

        var result = await hub.Get<PairDeviceHandler>().HandleAsync(new PairDeviceCommand(token.Token, "Chef 1", "android", "1.0.0"));

        result.IsSuccess.ShouldBeTrue();
        await using var db = await hub.DbAsync();
        var device = await db.Devices.SingleAsync();
        device.IsActive.ShouldBeTrue();
        device.SecretHash.ShouldNotBe(result.Value!.DeviceSecret);
        hub.Get<ISecretService>().Verify(result.Value.DeviceSecret, device.SecretHash).ShouldBeTrue();
    }

    [Fact]
    public async Task Pairing_token_cannot_be_reused()
    {
        await using var hub = await TestHub.CreateAsync();
        var token = await hub.Get<CreatePairingTokenHandler>().HandleAsync();
        var handler = hub.Get<PairDeviceHandler>();
        await handler.HandleAsync(new PairDeviceCommand(token.Token, "Chef 1", "android", "1.0.0"));

        var second = await handler.HandleAsync(new PairDeviceCommand(token.Token, "Chef 2", "android", "1.0.0"));

        second.Error!.Code.ShouldBe(ErrorCodes.PairingTokenInvalid);
    }

    [Fact]
    public async Task Expired_pairing_token_is_refused()
    {
        await using var hub = await TestHub.CreateAsync();
        var token = await hub.Get<CreatePairingTokenHandler>().HandleAsync();
        hub.Time.Advance(TimeSpan.FromMinutes(6));

        var result = await hub.Get<PairDeviceHandler>().HandleAsync(new PairDeviceCommand(token.Token, "Chef 1", "android", "1.0.0"));

        result.Error!.Code.ShouldBe(ErrorCodes.PairingTokenInvalid);
    }
}
