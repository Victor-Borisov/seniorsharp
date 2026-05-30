namespace SeniorSharp.Domain;

/// <summary>
/// Tracks a versioned prompt artifact used during a session for reproducibility.
/// </summary>
public class PromptVersion
{
    public Guid Id { get; set; }

    /// <summary>Logical prompt key, e.g. "questioner", "classifier", "scorer".</summary>
    public string Key { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    /// <summary>Content hash of the prompt for integrity checking.</summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>Source reference (e.g. git ref or file path).</summary>
    public string Ref { get; set; } = string.Empty;
}
