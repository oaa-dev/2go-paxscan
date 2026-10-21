namespace PaxScan.Desktop.Hosting;

/// <summary>Values resolved at startup (from the database) that Kestrel needs before it binds.</summary>
public sealed class HubRuntime
{
    public int Port { get; set; }

    public string CertificateFingerprint { get; set; } = string.Empty;
}
