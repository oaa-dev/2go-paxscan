namespace PaxScan.Application.Abstractions;

/// <summary>Serializes write transactions so SQLite never reports "database is locked".</summary>
public interface IWriteGate
{
    Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken = default);
}
