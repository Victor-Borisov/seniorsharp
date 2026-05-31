namespace SeniorSharp.Contracts;

/// <summary>
/// Request payload for the questioner role. Drives selection of the next skill
/// node to probe given the current mastery state and the relevant subgraph.
/// </summary>
/// <param name="MasteryStateJson">Serialized current mastery state (per-skill recognition/application/depth).</param>
/// <param name="SubgraphJson">Serialized relevant slice of the skill graph available for probing.</param>
/// <param name="AskedNodeIds">Skill node ids already asked in this session (avoid repetition).</param>
/// <param name="BudgetLeft">Remaining question budget for the current interview.</param>
/// <param name="Language">Human-readable language to conduct the interview in (empty = English).</param>
public sealed record QuestionerRequest(
    string MasteryStateJson,
    string SubgraphJson,
    string[] AskedNodeIds,
    int BudgetLeft,
    string Language = "");
