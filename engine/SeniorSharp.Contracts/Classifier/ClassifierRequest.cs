namespace SeniorSharp.Contracts;

/// <summary>
/// Request payload for the classifier role. Scores a single candidate answer
/// against a specific skill node along the recognition/application/depth dimensions.
/// </summary>
/// <param name="NodeJson">Serialized skill node being assessed.</param>
/// <param name="Question">The question that was asked.</param>
/// <param name="CandidateAnswer">The candidate's answer to classify.</param>
/// <param name="MasteryStateJson">Serialized current mastery state for context.</param>
/// <param name="Language">Human-readable language for the evidence/output (empty = English).</param>
public sealed record ClassifierRequest(
    string NodeJson,
    string Question,
    string CandidateAnswer,
    string MasteryStateJson,
    string Language = "");
