namespace SeniorSharp.Contracts;

/// <summary>
/// Score for a single mastery axis as produced by the scorer role.
/// </summary>
/// <param name="Axis">Mastery axis name (matches <c>MasteryAxis</c> in Domain).</param>
/// <param name="Level">Level assigned for this axis.</param>
/// <param name="Score">Numeric score for this axis (0..1).</param>
/// <param name="Rationale">Explanation for the assigned level/score.</param>
/// <param name="Citations">Transcript citations supporting the score.</param>
public sealed record AxisScoreDto(
    string Axis,
    string Level,
    double Score,
    string Rationale,
    string[] Citations);
