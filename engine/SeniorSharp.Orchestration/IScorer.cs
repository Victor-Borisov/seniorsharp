using SeniorSharp.Contracts;

namespace SeniorSharp.Orchestration;

/// <summary>
/// Produces per-axis scores and an overall verdict from the full interview
/// transcript and the scoring rubric.
/// </summary>
public interface IScorer
{
    Task<ScorerResponse> ScoreAsync(ScorerRequest request, CancellationToken ct = default);
}
