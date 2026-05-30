namespace SeniorSharp.Orchestration;

/// <summary>
/// Tunables for the interview loop. Bound from the "Interview" configuration section.
/// </summary>
public sealed class InterviewOptions
{
    public const string SectionName = "Interview";

    /// <summary>Number of questions to spend in the discovery round (experience/background).</summary>
    public int DiscoveryBudget { get; set; } = 2;

    /// <summary>Number of questions to spend in the deep-dive round.</summary>
    public int DeepDiveBudget { get; set; } = 5;

    /// <summary>Number of questions to spend in the system-design round.</summary>
    public int SystemDesignBudget { get; set; } = 2;

    /// <summary>The graph layer used as the system-design subgraph (and excluded from deep-dive).</summary>
    public string SystemDesignLayer { get; set; } = "Architecture & system design";

    /// <summary>Number of independent scorer runs to ensemble for the verdict (spread analysis).</summary>
    public int ScorerRuns { get; set; } = 3;

    /// <summary>
    /// Model used by the simulated candidate for sub-senior levels (junior/middle). A weaker/cheaper model
    /// produces more realistically sub-senior answers than the strong default playing a role. Senior/expert
    /// candidates keep the configured default model. Eval scaffolding only.
    /// </summary>
    public string CandidateModel { get; set; } = "claude-haiku-4-5";
}
