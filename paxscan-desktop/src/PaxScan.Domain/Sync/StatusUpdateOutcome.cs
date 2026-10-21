namespace PaxScan.Domain.Sync;

public sealed record StatusUpdateOutcome(
    UpdateBusinessStatus BusinessStatus,
    string? RejectReason,
    int? PreviousVersion,
    int? ResultingVersion)
{
    public static StatusUpdateOutcome Applied(UpdateBusinessStatus status, int previousVersion, int resultingVersion) =>
        new(status, null, previousVersion, resultingVersion);

    public static StatusUpdateOutcome Rejected(string reason) =>
        new(UpdateBusinessStatus.Rejected, reason, null, null);
}
