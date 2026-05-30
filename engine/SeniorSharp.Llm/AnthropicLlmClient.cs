using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;

namespace SeniorSharp.Llm;

/// <summary>
/// <see cref="ILlmClient"/> implementation backed by the official Anthropic SDK (NuGet package "Anthropic").
/// Structured completions use forced tool-use; the stable system prefix is cached via <c>cache_control</c>.
/// </summary>
public sealed class AnthropicLlmClient : ILlmClient
{
    /// <summary>
    /// ActivitySource name for orchestration tracing. Registered with OpenTelemetry in the API host so
    /// each LLM round-trip becomes a span exported to Langfuse.
    /// </summary>
    public const string ActivitySourceName = "SeniorSharp.Llm";

    private static readonly ActivitySource Activity = new(ActivitySourceName);

    // Tool-use input arrives keyed by the JSON-schema property names (camelCase); our DTOs are PascalCase
    // records, so deserialize with the camelCase policy and case-insensitive matching.
    private static readonly JsonSerializerOptions ResultJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly AnthropicClient _client;
    private readonly AnthropicOptions _options;

    public AnthropicLlmClient(AnthropicClient client, IOptions<AnthropicOptions> options)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<TResult> CompleteStructuredAsync<TResult>(
        IReadOnlyList<ChatMessage> messages,
        string jsonSchema,
        string toolName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(jsonSchema))
            throw new ArgumentException("A JSON schema is required.", nameof(jsonSchema));
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("A tool name is required.", nameof(toolName));

        // Parse the schema once into the raw shape the SDK's InputSchema expects.
        var schemaProps = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonSchema)
            ?? throw new ArgumentException("JSON schema did not parse to an object.", nameof(jsonSchema));

        var tool = new Tool
        {
            Name = toolName,
            InputSchema = InputSchema.FromRawUnchecked(schemaProps),
        };

        using var activity = Activity.StartActivity("llm.structured", ActivityKind.Client);
        activity?.SetTag("llm.model", _options.Model);
        activity?.SetTag("llm.tool", toolName);

        Exception? last = null;

        // Forced tool-use is reliable, but the model can still emit input that fails to deserialize into
        // TResult; retry a bounded number of times before giving up.
        for (var attempt = 0; attempt <= _options.StructuredRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var parameters = new MessageCreateParams
            {
                Model = _options.Model,
                MaxTokens = _options.MaxTokens,
                Messages = ToMessageParams(messages),
                Tools = new List<ToolUnion> { tool },
                ToolChoice = new ToolChoice(new ToolChoiceTool(toolName)),
                System = BuildSystem(messages),
            };

            try
            {
                var response = await _client.Messages.Create(parameters, ct);

                var toolUse = response.Content
                    .Select(b => b.Value)
                    .OfType<ToolUseBlock>()
                    .FirstOrDefault(t => string.Equals(t.Name, toolName, StringComparison.Ordinal));

                if (toolUse is null)
                    throw new InvalidOperationException(
                        $"Model returned no tool-use block for '{toolName}' (stop reason: {response.StopReason}).");

                var inputJson = JsonSerializer.Serialize(toolUse.Input);
                var result = JsonSerializer.Deserialize<TResult>(inputJson, ResultJson);

                if (result is null)
                    throw new InvalidOperationException("Tool-use input deserialized to null.");

                activity?.SetTag("llm.attempts", attempt + 1);
                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                last = ex;
                activity?.AddEvent(new ActivityEvent($"retry.{attempt}: {ex.Message}"));
            }
        }

        activity?.SetStatus(ActivityStatusCode.Error, last?.Message);
        throw new InvalidOperationException(
            $"Structured completion for tool '{toolName}' failed after {_options.StructuredRetries + 1} attempt(s).",
            last);
    }

    /// <inheritdoc />
    public async Task<string> CompleteTextAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct = default,
        string? modelOverride = null)
    {
        var model = string.IsNullOrWhiteSpace(modelOverride) ? _options.Model : modelOverride;

        using var activity = Activity.StartActivity("llm.text", ActivityKind.Client);
        activity?.SetTag("llm.model", model);

        var parameters = new MessageCreateParams
        {
            Model = model,
            MaxTokens = _options.MaxTokens,
            Messages = ToMessageParams(messages),
            System = BuildSystem(messages),
        };

        var response = await _client.Messages.Create(parameters, ct);

        return string.Concat(response.Content
            .Select(b => b.Value)
            .OfType<TextBlock>()
            .Select(t => t.Text));
    }

    /// <summary>
    /// Concatenates <see cref="ChatRole.System"/> messages into a single stable prefix block tagged with
    /// <c>cache_control: ephemeral</c> so repeated calls in a session reuse it (prompt caching).
    /// Returns null when there is no system content.
    /// </summary>
    private static SystemModel? BuildSystem(IReadOnlyList<ChatMessage> messages)
    {
        var systemText = string.Join(
            "\n\n",
            messages.Where(m => m.Role == ChatRole.System).Select(m => m.Content));

        if (string.IsNullOrEmpty(systemText))
            return null;

        // A single stable prefix block tagged ephemeral so repeated session calls reuse it (prompt caching).
        return new List<TextBlockParam>
        {
            new()
            {
                Text = systemText,
                CacheControl = new CacheControlEphemeral(), // {"type":"ephemeral"}
            },
        };
    }

    /// <summary>
    /// Maps non-system <see cref="ChatMessage"/>s to SDK <c>MessageParam</c>s (user/assistant turns).
    /// </summary>
    private static List<MessageParam> ToMessageParams(IReadOnlyList<ChatMessage> messages)
    {
        return messages
            .Where(m => m.Role != ChatRole.System)
            .Select(m => new MessageParam
            {
                Role = m.Role == ChatRole.Assistant ? Role.Assistant : Role.User,
                Content = m.Content,
            })
            .ToList();
    }
}
