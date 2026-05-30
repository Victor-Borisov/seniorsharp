using System.Threading;
using System.Threading.Tasks;

namespace SeniorSharp.Orchestration;

/// <summary>
/// Produces a candidate answer to an interview question. The simulated implementation lets the
/// deep-dive loop run end-to-end without a human, and is reused by the M4 autoeval (synthetic
/// profiles of a known level). NOT a production interview role.
/// </summary>
public interface ICandidate
{
    /// <summary>
    /// Answers <paramref name="question"/> as a developer of the given <paramref name="level"/>
    /// (e.g. "middle", "senior"). <paramref name="nodeContext"/> is the skill node being probed.
    /// </summary>
    Task<string> AnswerAsync(string question, string nodeContext, string level, CancellationToken ct = default);
}
