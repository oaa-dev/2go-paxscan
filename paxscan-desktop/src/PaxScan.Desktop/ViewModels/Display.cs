using System.Globalization;
using PaxScan.Application.Common;

namespace PaxScan.Desktop.ViewModels;

internal static class Display
{
    public static string Time(DateTimeOffset? value) =>
        value is { } v ? v.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) : "—";

    public static string Ago(DateTimeOffset? value, DateTimeOffset now)
    {
        if (value is not { } v)
        {
            return "never";
        }

        var span = now - v;
        return span.TotalSeconds < 60 ? $"{Math.Max(0, (int)span.TotalSeconds)}s ago"
            : span.TotalMinutes < 60 ? $"{(int)span.TotalMinutes}m ago"
            : span.TotalHours < 24 ? $"{(int)span.TotalHours}h ago"
            : Time(v);
    }

    public static string Wire<TEnum>(TEnum value)
        where TEnum : struct, Enum => WireJson.EnumValue(value);
}
