namespace SeniorSharp.Domain;

/// <summary>
/// A node in the skill graph (closed-moat content). Identified by a stable string Id.
/// </summary>
public class SkillNode
{
    /// <summary>Stable primary key, e.g. "async.tap".</summary>
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    /// <summary>Graph layer / cluster the node belongs to.</summary>
    public string Layer { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Mastery axes this node contributes evidence to.</summary>
    public string[] Axes { get; set; } = [];

    /// <summary>Ids of prerequisite skill nodes.</summary>
    public string[] Prerequisites { get; set; } = [];

    public string[] MasteryFocus { get; set; } = [];

    public string SeniorSignal { get; set; } = string.Empty;

    public string ExampleProbe { get; set; } = string.Empty;

    /// <summary>Version of the skill graph this node was seeded from.</summary>
    public string GraphVersion { get; set; } = string.Empty;
}
