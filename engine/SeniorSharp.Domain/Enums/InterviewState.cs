namespace SeniorSharp.Domain;

/// <summary>
/// FSM state for an interview session.
/// Transitions: Created -> Discovery -> DeepDive -> SystemDesign -> Scoring -> Report -> Done.
/// </summary>
public enum InterviewState
{
    Created,
    Discovery,
    DeepDive,
    SystemDesign,
    Scoring,
    Report,
    Done
}
