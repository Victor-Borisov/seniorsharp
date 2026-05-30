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
    /// Model identifier. Current most-capable Anthropic model.
    /// TODO: keep in sync with the latest Claude Opus id; as of this writing the current id is "claude-opus-4-8".
    /// </summary>
    public string Model { get; set; } = "claude-opus-4-8";

    /// <summary>Maximum output tokens per completion. Streaming is recommended above ~16000.</summary>
    public int MaxTokens { get; set; } = 16000;

    /// <summary>Number of retries for structured completions whose result fails schema validation / deserialization.</summary>
    public int StructuredRetries { get; set; } = 2;
}
