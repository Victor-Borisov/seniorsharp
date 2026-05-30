using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeniorSharp.Contracts;
using SeniorSharp.Llm;

namespace SeniorSharp.Orchestration;

/// <summary>
/// LLM-backed scorer. Forces structured tool-use against
/// <see cref="PromptSchemas.ScorerJsonSchema"/>. Intended to be run multiple
/// times for ensembling / spread analysis.
/// </summary>
public sealed class Scorer : IScorer
{
    private const string ToolName = "emit_score";
    private const string PromptRole = "scorer";
    private const string PromptVersion = "v1";

    private readonly ILlmClient _llm;
    private readonly IPromptProvider _prompts;
    private readonly ILogger<Scorer> _logger;

    public Scorer(ILlmClient llm, IPromptProvider prompts, ILogger<Scorer> logger)
    {
        _llm = llm;
        _prompts = prompts;
        _logger = logger;
    }

    public async Task<ScorerResponse> ScoreAsync(ScorerRequest request, CancellationToken ct = default)
    {
        // Stable, cacheable prefix: the role prompt + the rubric criteria + the axis list. The transcript
        // is the only volatile part, so it goes in the user turn (keeps the prompt-cache key byte-stable).
        var system = _prompts.GetSystemPrompt(PromptRole, PromptVersion);
        var axes = string.Join(", ", request.Axes);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, system),
            new(ChatRole.System, $"## Scoring criteria\n\n{request.CriteriaJson}\n\n## Axes to score\n\n{axes}"),
            new(ChatRole.User, $"## Interview transcript\n\n{request.TranscriptJson}"),
        };

        _logger.LogInformation("Scoring transcript over {AxisCount} axes ({Axes}).", request.Axes.Length, axes);

        return await _llm.CompleteStructuredAsync<ScorerResponse>(
            messages, PromptSchemas.ScorerJsonSchema, ToolName, ct);
    }
}
