namespace PaxScan.Domain.Settings;

/// <summary>Single-row hub configuration.</summary>
public sealed class HubSettings
{
    public const int SingletonId = 1;
    public const int DefaultPort = 5443;

    private HubSettings()
    {
    }

    public int Id { get; private set; }

    public Guid HubId { get; private set; }

    public string? WebBaseUrl { get; private set; }

    /// <summary>Online hub token, encrypted with DPAPI.</summary>
    public byte[]? HubTokenProtected { get; private set; }

    /// <summary>LAN address put in the pairing QR code.</summary>
    public string? AdvertisedHost { get; private set; }

    public int Port { get; private set; }

    public DateTimeOffset? LastWebSyncAt { get; private set; }

    public string? DatasetVersion { get; private set; }

    public static HubSettings CreateDefault(Guid hubId) =>
        new() { Id = SingletonId, HubId = hubId, Port = DefaultPort };

    public void UpdateConnection(string? webBaseUrl, byte[]? hubTokenProtected, string? advertisedHost, int port)
    {
        WebBaseUrl = string.IsNullOrWhiteSpace(webBaseUrl) ? null : webBaseUrl.TrimEnd('/');
        if (hubTokenProtected is not null)
        {
            HubTokenProtected = hubTokenProtected;
        }

        AdvertisedHost = string.IsNullOrWhiteSpace(advertisedHost) ? null : advertisedHost.Trim();
        Port = port;
    }

    public void RecordWebSync(string? datasetVersion, DateTimeOffset now)
    {
        DatasetVersion = datasetVersion;
        LastWebSyncAt = now;
    }
}
