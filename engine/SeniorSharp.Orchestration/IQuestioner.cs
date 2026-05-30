using SeniorSharp.Contracts;

namespace SeniorSharp.Orchestration;

/// <summary>
/// Selects the next skill node to probe and produces the interviewer question
/// for the current round, given the candidate's evolving mastery state.
/// </summary>
public interface IQuestioner
{
    Task<QuestionerResponse> AskAsync(QuestionerRequest request, CancellationToken ct = default);
}
