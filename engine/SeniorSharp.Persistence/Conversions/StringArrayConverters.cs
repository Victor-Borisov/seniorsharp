using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SeniorSharp.Persistence.Conversions;

/// <summary>
/// Shared value converter + comparer for <c>string[]</c> properties persisted as PostgreSQL <c>jsonb</c>.
/// EF Core needs an explicit <see cref="ValueComparer{T}"/> for reference-typed mutable values
/// (arrays) so that change tracking detects in-place mutations and snapshots correctly.
/// </summary>
public static class StringArrayConverters
{
    public static readonly ValueConverter<string[], string> Converter = new(
        v => JsonSerializer.Serialize(v ?? Array.Empty<string>(), JsonOptions),
        v => string.IsNullOrEmpty(v)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(v, JsonOptions) ?? Array.Empty<string>());

    public static readonly ValueComparer<string[]> Comparer = new(
        (a, b) => (a ?? Array.Empty<string>()).SequenceEqual(b ?? Array.Empty<string>()),
        v => v == null ? 0 : v.Aggregate(0, (acc, s) => HashCode.Combine(acc, s == null ? 0 : s.GetHashCode())),
        v => v == null ? Array.Empty<string>() : v.ToArray());

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
