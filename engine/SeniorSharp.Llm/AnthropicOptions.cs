namespace SeniorSharp.Llm;

/// <summary>
/// Configuration for the Anthropic LLM provider. Bound from configuration / environment.
/// </summary>
public sealed class AnthropicOptions
{
    /// <summary>Configuration section name for binding.</summary>
    public const string SectionName = "Anthropic";

    /// <summary>Anthropic API key. Sourced from configuration / environment (e.g. ANTHROPIC_API_KEY); never hardcode.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model identifier used for all roles unless overridden. Default is Sonnet: a deliberate cost choice for
    /// the public demo (~5x cheaper than Opus at comparable screening quality). Production also pins Sonnet via
    /// the Anthropic__Model environment variable, so this default keeps repo == prod and avoids an accidental
    /// Opus run from a fresh clone. Switch to "claude-opus-4-8" only when verdict quality demands it.
    /// </summary>
    public string Model { get; set; } = "claude-sonnet-4-6";

    /// <summary>Maximum output tokens per completion. Streaming is recommended above ~16000.</summary>
    public int MaxTokens { get; set; } = 16000;

    /// <summary>Number of retries for structured completions whose result fails schema validation / deserialization.</summary>
    public int StructuredRetries { get; set; } = 2;
}
