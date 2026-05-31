namespace SeniorSharp.Contracts;

/// <summary>
/// Request payload for the scorer role. Produces the final per-axis assessment
/// over the full interview transcript using the supplied rubric criteria.
/// </summary>
/// <param name="TranscriptJson">Serialized full interview transcript.</param>
/// <param name="CriteriaJson">Serialized rubric/criteria to score against.</param>
/// <param name="Axes">Mastery axes to produce scores for.</param>
/// <param name="Language">Human-readable language for the rationale/summary (empty = English).</param>
public sealed record ScorerRequest(
    string TranscriptJson,
    string CriteriaJson,
    string[] Axes,
    string Language = "");
