using SeniorSharp.Domain;

namespace SeniorSharp.Orchestration;

/// <summary>
/// Drives a single interview through the FSM: starts a session, accepts
/// candidate answers (classifying each and choosing the next probe), and
/// advances rounds until a verdict is produced.
/// </summary>
public interface IInterviewOrchestrator
{
    /// <summary>Creates a new session and emits the first interviewer turn.</summary>
    Task<StartInterviewResult> StartAsync(StartInterviewRequest request, CancellationToken ct = default);

    /// <summary>
    /// Records a candidate answer for the active round, classifies it, and
    /// returns the next interviewer turn (or signals the round/interview is done).
    /// </summary>
    Task<SubmitAnswerResult> SubmitAnswerAsync(SubmitAnswerRequest request, CancellationToken ct = default);

    /// <summary>
    /// Advances the session to the next FSM state (e.g. close a round, move to
    /// scoring, produce the report). Idempotent per target state.
    /// </summary>
    Task<AdvanceResult> AdvanceAsync(Guid sessionId, CancellationToken ct = default);
}

/// <summary>Input for <see cref="IInterviewOrchestrator.StartAsync"/>.</summary>
public sealed record StartInterviewRequest(string? CandidateRef, string? GraphVersion = null, string? Language = null);

/// <summary>Result of starting an interview.</summary>
public sealed record StartInterviewResult(
    Guid SessionId,
    InterviewState State,
    string FirstQuestion);

/// <summary>Input for <see cref="IInterviewOrchestrator.SubmitAnswerAsync"/>.</summary>
public sealed record SubmitAnswerRequest(Guid SessionId, string AnswerText);

/// <summary>Result of submitting a candidate answer.</summary>
public sealed record SubmitAnswerResult(
    Guid SessionId,
    InterviewState State,
    string? NextQuestion,
    bool RoundComplete);

/// <summary>Result of advancing the FSM.</summary>
/// <param name="NextQuestion">
/// First question of the round just opened, when the advance moved into an interviewing round;
/// null when the advance moved into Scoring/Report/Done.
/// </param>
public sealed record AdvanceResult(
    Guid SessionId,
    InterviewState State,
    bool IsTerminal,
    string? NextQuestion = null);
