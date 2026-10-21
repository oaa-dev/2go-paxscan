namespace PaxScan.Application.Sync;

public static class ItemResultCodes
{
    public const string Accepted = "accepted";
    public const string AlreadyReceived = "already_received";
    public const string Conflict = "conflict";
    public const string Rejected = "rejected";
}

public sealed record ItemResult(Guid Id, string Result, string? Reason, int? TaskVersion);
