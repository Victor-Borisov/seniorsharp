using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SeniorSharp.Domain;

namespace SeniorSharp.Persistence;

/// <summary>
/// Reads <c>content/skill-graph.json</c> and upserts its nodes into <see cref="AppDbContext.SkillNodes"/>.
/// The JSON shape is: <c>{ version, date, nodes: [{ id, title, layer, description, axes[],
/// prerequisites[], mastery_focus[], senior_signal, example_probe, provenance? }] }</c>.
/// </summary>
public sealed class GraphSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly AppDbContext _db;

    public GraphSeeder(AppDbContext db) => _db = db;

    /// <summary>
    /// Loads the graph from the given file path and upserts all nodes.
    /// Returns the number of nodes processed (inserted or updated).
    /// </summary>
    public async Task<int> SeedFromFileAsync(string graphJsonPath, CancellationToken ct = default)
    {
        if (!File.Exists(graphJsonPath))
            throw new FileNotFoundException($"skill-graph.json not found at '{graphJsonPath}'.", graphJsonPath);

        await using var stream = File.OpenRead(graphJsonPath);
        return await SeedFromStreamAsync(stream, ct);
    }

    /// <summary>
    /// Loads the graph from a stream and upserts all nodes by <c>Id</c>.
    /// </summary>
    public async Task<int> SeedFromStreamAsync(Stream stream, CancellationToken ct = default)
    {
        var graph = await JsonSerializer.DeserializeAsync<GraphDocument>(stream, JsonOptions, ct)
                    ?? throw new InvalidOperationException("Failed to deserialize skill-graph.json (null root).");

        if (graph.Nodes is null || graph.Nodes.Count == 0)
            return 0;

        var version = graph.Version ?? "unknown";
        var ids = graph.Nodes.Select(n => n.Id).Where(id => !string.IsNullOrEmpty(id)).ToArray();

        var existing = await _db.SkillNodes
            .Where(n => ids.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, ct);

        var processed = 0;
        foreach (var dto in graph.Nodes)
        {
            if (string.IsNullOrEmpty(dto.Id))
                continue;

            if (!existing.TryGetValue(dto.Id, out var entity))
            {
                entity = new SkillNode { Id = dto.Id };
                _db.SkillNodes.Add(entity);
            }

            entity.Title = dto.Title ?? string.Empty;
            entity.Layer = dto.Layer ?? string.Empty;
            entity.Description = dto.Description ?? string.Empty;
            entity.Axes = dto.Axes ?? Array.Empty<string>();
            entity.Prerequisites = dto.Prerequisites ?? Array.Empty<string>();
            entity.MasteryFocus = dto.MasteryFocus ?? Array.Empty<string>();
            entity.SeniorSignal = dto.SeniorSignal ?? string.Empty;
            entity.ExampleProbe = dto.ExampleProbe ?? string.Empty;
            entity.GraphVersion = version;

            processed++;
        }

        await _db.SaveChangesAsync(ct);
        return processed;
    }

    // --- JSON DTOs mirroring the on-disk skill-graph.json shape (snake_case in file) ---

    private sealed class GraphDocument
    {
        [JsonPropertyName("version")] public string? Version { get; init; }
        [JsonPropertyName("date")] public string? Date { get; init; }
        [JsonPropertyName("nodes")] public List<GraphNode>? Nodes { get; init; }
    }

    private sealed class GraphNode
    {
        [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
        [JsonPropertyName("title")] public string? Title { get; init; }
        [JsonPropertyName("layer")] public string? Layer { get; init; }
        [JsonPropertyName("description")] public string? Description { get; init; }
        [JsonPropertyName("axes")] public string[]? Axes { get; init; }
        [JsonPropertyName("prerequisites")] public string[]? Prerequisites { get; init; }
        [JsonPropertyName("mastery_focus")] public string[]? MasteryFocus { get; init; }
        [JsonPropertyName("senior_signal")] public string? SeniorSignal { get; init; }
        [JsonPropertyName("example_probe")] public string? ExampleProbe { get; init; }
        [JsonPropertyName("provenance")] public JsonElement? Provenance { get; init; }
    }
}
