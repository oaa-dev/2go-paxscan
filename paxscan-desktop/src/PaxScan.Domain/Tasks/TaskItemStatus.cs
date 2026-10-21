namespace PaxScan.Domain.Tasks;

/// <summary>Wire values are snake_case: <c>pending</c>, <c>in_progress</c>, <c>done</c>.</summary>
public enum TaskItemStatus
{
    Pending,
    InProgress,
    Done,
}
