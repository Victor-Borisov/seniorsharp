namespace SeniorSharp.Orchestration;

/// <summary>
/// Supplies versioned system prompts for the interview roles. Prompts live in the closed-content
/// set and are versioned as code (see <c>content/README.md</c>).
/// </summary>
public interface IPromptProvider
{
    /// <summary>
    /// Returns the system prompt text for a role/version pair, e.g. ("scorer", "v1").
    /// </summary>
    string GetSystemPrompt(string role, string version);

    /// <summary>Returns the scoring criteria document text.</summary>
    string GetCriteria();
}
