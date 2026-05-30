namespace SeniorSharp.Orchestration;

/// <summary>
/// Locates the closed-content prompt set on disk. Bound from the "Content" configuration section.
/// </summary>
public sealed class PromptOptions
{
    /// <summary>Configuration section name for binding.</summary>
    public const string SectionName = "Content";

    /// <summary>
    /// Directory holding versioned prompt files named <c>{role}.{version}.md</c>
    /// (e.g. <c>scorer.v1.md</c>). Relative paths resolve against the process working directory.
    /// </summary>
    public string PromptsDir { get; set; } = "content/prompts";

    /// <summary>Path to the scoring criteria document. Relative paths resolve against the working directory.</summary>
    public string CriteriaPath { get; set; } = "content/criteria.md";
}
