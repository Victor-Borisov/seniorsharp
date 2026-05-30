using SeniorSharp.Domain;

namespace SeniorSharp.Orchestration;

/// <summary>
/// Pure, deterministic transition logic for the interview FSM.
/// Linear progression:
/// Created -> Discovery -> DeepDive -> SystemDesign -> Scoring -> Report -> Done.
/// Any state may transition to itself (no-op) is NOT allowed; transitions are explicit.
/// </summary>
public static class InterviewStateMachine
{
    /// <summary>
    /// Returns the next state in the canonical interview flow, or null when the
    /// machine is already in a terminal state (<see cref="InterviewState.Done"/>).
    /// </summary>
    public static InterviewState? Next(InterviewState current) => current switch
    {
        InterviewState.Created => InterviewState.Discovery,
        InterviewState.Discovery => InterviewState.DeepDive,
        InterviewState.DeepDive => InterviewState.SystemDesign,
        InterviewState.SystemDesign => InterviewState.Scoring,
        InterviewState.Scoring => InterviewState.Report,
        InterviewState.Report => InterviewState.Done,
        InterviewState.Done => null,
        _ => null
    };

    /// <summary>
    /// Validates whether a transition from <paramref name="from"/> to
    /// <paramref name="to"/> is allowed by the FSM.
    /// </summary>
    public static bool CanTransition(InterviewState from, InterviewState to)
        => Next(from) is { } next && next == to;

    /// <summary>
    /// Advances <paramref name="current"/> to the next state, throwing if the
    /// machine is already terminal.
    /// </summary>
    public static InterviewState Advance(InterviewState current)
        => Next(current) ?? throw new InvalidOperationException(
            $"Cannot advance interview state machine: '{current}' is terminal.");

    /// <summary>True when no further transitions are possible.</summary>
    public static bool IsTerminal(InterviewState state) => Next(state) is null;

    /// <summary>
    /// Maps an interview state to the round type that should be active during it,
    /// when applicable. Returns null for non-interviewing states.
    /// </summary>
    public static RoundType? RoundFor(InterviewState state) => state switch
    {
        InterviewState.Discovery => RoundType.Discovery,
        InterviewState.DeepDive => RoundType.DeepDive,
        InterviewState.SystemDesign => RoundType.SystemDesign,
        _ => null
    };
}
