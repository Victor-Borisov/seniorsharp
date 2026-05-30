namespace SeniorSharp.Contracts;

/// <summary>
/// Structured response from the scorer role: per-axis scores plus an overall verdict.
/// Shape MUST stay in sync with <see cref="PromptSchemas.ScorerJsonSchema"/>.
/// </summary>
/// <param name="Axes">Per-axis score breakdown.</param>
/// <param name="OverallLevel">Overall seniority level verdict (e.g. Junior/Middle/Senior).</param>
/// <param name="Summary">Human-readable summary justifying the verdict.</param>
public sealed record ScorerResponse(
    AxisScoreDto[] Axes,
    string OverallLevel,
    string Summary);
