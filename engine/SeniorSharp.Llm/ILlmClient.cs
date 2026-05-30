using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SeniorSharp.Llm;

/// <summary>
/// Provider-agnostic abstraction over an LLM chat completion endpoint.
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Performs a structured completion using forced tool-use: a single tool is exposed with
    /// <paramref name="jsonSchema"/> as its <c>input_schema</c>, <c>tool_choice</c> is forced to that
    /// tool, and the resulting tool-use input is deserialized into <typeparamref name="TResult"/>.
    /// Implementations should validate the deserialized result against the schema and retry on failure.
    /// </summary>
    /// <typeparam name="TResult">DTO matching <paramref name="jsonSchema"/>.</typeparam>
    /// <param name="messages">The conversation, including any system prompt.</param>
    /// <param name="jsonSchema">A valid JSON Schema string describing the tool input / expected result.</param>
    /// <param name="toolName">The name of the forced tool.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<TResult> CompleteStructuredAsync<TResult>(
        IReadOnlyList<ChatMessage> messages,
        string jsonSchema,
        string toolName,
        CancellationToken ct = default);

    /// <summary>
    /// Performs a plain text completion and returns the concatenated text content.
    /// <paramref name="modelOverride"/> selects a model other than the configured default (e.g. a cheaper
    /// model to role-play a weaker candidate in the eval harness); null uses the configured model.
    /// </summary>
    Task<string> CompleteTextAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct = default,
        string? modelOverride = null);
}
