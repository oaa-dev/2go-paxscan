using System.Security.Cryptography;
using System.Text;

namespace PaxScan.Application.Common;

/// <summary>sha256 hex of task <c>id:version</c> pairs sorted by id, joined with "\n" (see contract).</summary>
public static class TaskChecksum
{
    public static string Compute(IEnumerable<(Guid Id, int Version)> tasks)
    {
        var lines = tasks
            .Select(t => $"{t.Id.ToString("D").ToLowerInvariant()}:{t.Version}")
            .Order(StringComparer.Ordinal);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', lines)));
        return Convert.ToHexStringLower(bytes);
    }
}
