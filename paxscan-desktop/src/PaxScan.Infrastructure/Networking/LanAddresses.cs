using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PaxScan.Infrastructure.Networking;

public static class LanAddresses
{
    /// <summary>Windows Mobile Hotspot always gives the laptop this address.</summary>
    public const string MobileHotspotAddress = "192.168.137.1";

    /// <summary>IPv4 addresses of interfaces that are up (no loopback), hotspot address first.</summary>
    public static IReadOnlyList<string> GetIPv4Addresses() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Select(a => a.Address)
            .Where(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a))
            .Select(a => a.ToString())
            .Distinct()
            .OrderBy(a => a == MobileHotspotAddress ? 0 : 1)
            .ThenBy(a => a, StringComparer.Ordinal)
            .ToList();
}
