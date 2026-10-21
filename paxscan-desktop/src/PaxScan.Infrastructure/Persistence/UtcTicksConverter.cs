using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PaxScan.Infrastructure.Persistence;

/// <summary>
/// SQLite cannot compare or order DateTimeOffset values in SQL; storing UTC ticks keeps
/// ordering (hub_received_at) correct and server-side.
/// </summary>
internal sealed class UtcTicksConverter()
    : ValueConverter<DateTimeOffset, long>(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));
