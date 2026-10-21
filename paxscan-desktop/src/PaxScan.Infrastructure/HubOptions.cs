namespace PaxScan.Infrastructure;

public sealed class HubOptions
{
    /// <summary>Where the database, certificate and logs live (%LOCALAPPDATA%\PaxScan by default).</summary>
    public string DataDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PaxScan");

    /// <summary>Overrides the SQLite connection string (tests use a shared in-memory database).</summary>
    public string? ConnectionString { get; set; }

    public string ResolveConnectionString() =>
        ConnectionString ?? $"Data Source={Path.Combine(DataDirectory, "hub.db")}";
}
