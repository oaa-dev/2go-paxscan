using PaxScan.Domain.Tasks;

namespace PaxScan.Application.Abstractions;

/// <summary>Client for the Online hub endpoints (<c>/api/v1/hub/*</c>).</summary>
public interface IWebApiClient
{
    Task<WebTaskSnapshot> GetTaskSnapshotAsync(WebConnection connection, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebItemResult>> PushTaskUpdatesAsync(
        WebConnection connection,
        Guid hubId,
        IReadOnlyList<WebStatusUpdate> items,
        CancellationToken cancellationToken = default);
}

public sealed record WebConnection(Uri BaseUrl, string Token);

public sealed record WebTaskSnapshot(string? DatasetVersion, string Checksum, IReadOnlyList<WebTaskFields> Tasks);

public sealed record WebStatusUpdate(
    Guid Id,
    Guid TaskId,
    TaskItemStatus FromStatus,
    TaskItemStatus ToStatus,
    int BaseVersion,
    Guid DeviceId,
    long DeviceSeq,
    DateTimeOffset DeviceCreatedAt,
    DateTimeOffset HubReceivedAt);

public sealed record WebItemResult(Guid Id, string Result, string? Reason, int? TaskVersion);

/// <summary>Online could not be reached or answered with an unexpected response.</summary>
public sealed class WebApiException : Exception
{
    public WebApiException()
    {
    }

    public WebApiException(string message)
        : base(message)
    {
    }

    public WebApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public int? StatusCode { get; init; }
}
