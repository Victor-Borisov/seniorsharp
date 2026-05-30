using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SeniorSharp.Persistence.Conversions;

/// <summary>
/// Value converter + comparer for <c>List&lt;Guid&gt;</c> properties persisted as PostgreSQL <c>jsonb</c>
/// (e.g. SkillMastery.EvidenceTurnIds). Mirrors <see cref="StringArrayConverters"/>.
/// </summary>
public static class GuidListConverters
{
    public static readonly ValueConverter<List<Guid>, string> Converter = new(
        v => JsonSerializer.Serialize(v ?? new List<Guid>(), JsonOptions),
        v => string.IsNullOrEmpty(v)
            ? new List<Guid>()
            : JsonSerializer.Deserialize<List<Guid>>(v, JsonOptions) ?? new List<Guid>());

    public static readonly ValueComparer<List<Guid>> Comparer = new(
        (a, b) => (a ?? new List<Guid>()).SequenceEqual(b ?? new List<Guid>()),
        v => v == null ? 0 : v.Aggregate(0, (acc, g) => HashCode.Combine(acc, g.GetHashCode())),
        v => v == null ? new List<Guid>() : v.ToList());

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
