using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using PaxScan.Application.Common;
using PaxScan.Application.Devices;
using PaxScan.Application.Pairing;
using PaxScan.Application.Sync;
using PaxScan.Domain.Sync;

namespace PaxScan.HubApi;

internal static class HubEndpoints
{
    private static readonly string[] Platforms = ["android", "ios"];

    public static IEndpointRouteBuilder MapHubEndpoints(this IEndpointRouteBuilder app)
    {
        var v1 = app.MapGroup("/v1");

        v1.MapPost("/pair", PairAsync)
            .AllowAnonymous()
            .RequireRateLimiting(HubApiExtensions.PairRateLimitPolicy);

        var device = v1.MapGroup(string.Empty).RequireAuthorization();
        device.MapPost("/ping", PingAsync);
        device.MapPost("/sync/hello", HelloAsync);
        device.MapPost("/sync/push", PushAsync);
        device.MapGet("/sync/snapshot", SnapshotAsync);

        return app;
    }

    private static async Task<Results<Ok<PairResponse>, ProblemHttpResult>> PairAsync(
        PairRequest request,
        PairDeviceHandler handler,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PairingToken) || string.IsNullOrWhiteSpace(request.DeviceName)
            || request.Platform is null || !Platforms.Contains(request.Platform) || string.IsNullOrWhiteSpace(request.AppVersion))
        {
            return Problem(StatusCodes.Status400BadRequest, "pairing_token, device_name, platform (android|ios) and app_version are required.");
        }

        var result = await handler.HandleAsync(
            new PairDeviceCommand(request.PairingToken, request.DeviceName, request.Platform, request.AppVersion),
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(new PairResponse(result.Value!.DeviceId, result.Value.DeviceSecret, result.Value.HubId))
            : ToProblem(result.Error!);
    }

    private static async Task<Results<Ok<PingResponse>, ProblemHttpResult>> PingAsync(
        PingRequest request,
        ClaimsPrincipal user,
        RecordHeartbeatHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(user.DeviceId(), request.PendingCount, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(new PingResponse(result.Value!.SyncRequested, result.Value.ServerTime))
            : ToProblem(result.Error!);
    }

    private static async Task<Results<Ok<HelloResponse>, ProblemHttpResult>> HelloAsync(
        HelloRequest request,
        ClaimsPrincipal user,
        HelloHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new HelloCommand(user.DeviceId(), request.LastSeq, request.PendingCount, request.DeviceTime),
            cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(new HelloResponse(result.Value!.ExpectedFromSeq, result.Value.ServerTime))
            : ToProblem(result.Error!);
    }

    private static async Task<Results<Ok<PushResponse>, ProblemHttpResult>> PushAsync(
        PushRequest request,
        ClaimsPrincipal user,
        PushUpdatesHandler handler,
        CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Any(i => i.DeviceSeq < 1 || i.BaseVersion < 1 || i.Id == Guid.Empty))
        {
            return Problem(StatusCodes.Status400BadRequest, "items are required; device_seq and base_version must be ≥ 1.");
        }

        var items = request.Items
            .Select(i => new DeviceStatusUpdate(i.Id, i.TaskId, i.FromStatus, i.ToStatus, i.BaseVersion, i.DeviceSeq, i.DeviceCreatedAt))
            .ToList();
        var result = await handler.HandleAsync(user.DeviceId(), items, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(new PushResponse(result.Value!.Results, result.Value.SeqGapDetected))
            : ToProblem(result.Error!);
    }

    private static async Task<Ok<SnapshotResponse>> SnapshotAsync(BuildSnapshotHandler handler, CancellationToken cancellationToken)
    {
        var snapshot = await handler.HandleAsync(cancellationToken);
        return TypedResults.Ok(new SnapshotResponse(snapshot.DatasetVersion, snapshot.Tasks.Select(TaskDto.From).ToList()));
    }

    private static ProblemHttpResult ToProblem(AppError error) => error.Code switch
    {
        ErrorCodes.PairingTokenInvalid or ErrorCodes.DeviceRevoked or ErrorCodes.DeviceNotFound =>
            Problem(StatusCodes.Status401Unauthorized, error.Message, error.Code),
        _ => Problem(StatusCodes.Status400BadRequest, error.Message, error.Code),
    };

    private static ProblemHttpResult Problem(int status, string detail, string? code = null) =>
        TypedResults.Problem(
            detail: detail,
            statusCode: status,
            extensions: code is null ? null : new Dictionary<string, object?> { ["code"] = code });
}
