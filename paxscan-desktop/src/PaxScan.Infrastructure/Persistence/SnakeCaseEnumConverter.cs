using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PaxScan.Infrastructure.Persistence;

/// <summary>Stores enums as their wire values (e.g. <c>in_progress</c>) so the DB reads like the API.</summary>
internal sealed class SnakeCaseEnumConverter<TEnum>()
    : ValueConverter<TEnum, string>(v => ToWire(v), v => FromWire(v))
    where TEnum : struct, Enum
{
    private static readonly Dictionary<string, TEnum> ByWire =
        Enum.GetValues<TEnum>().ToDictionary(ToWire, v => v, StringComparer.Ordinal);

    private static string ToWire(TEnum value) => JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());

    private static TEnum FromWire(string value) => ByWire[value];
}
