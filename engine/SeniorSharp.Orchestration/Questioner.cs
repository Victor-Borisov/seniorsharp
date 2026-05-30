using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeniorSharp.Contracts;
using SeniorSharp.Llm;

namespace SeniorSharp.Orchestration;

/// <summary>
/// LLM-backed questioner. Forces structured tool-use against
/// <see cref="PromptSchemas.QuestionerJsonSchema"/> to pick the next node + question.
/// </summary>
public sealed class Questioner : IQuestioner
{
    private const string ToolName = "emit_next_question";
    private const string PromptRole = "questioner";
    private const string PromptVersion = "v1";

    private readonly ILlmClient _llm;
    private readonly IPromptProvider _prompts;
    private readonly ILogger<Questioner> _logger;

    public Questioner(ILlmClient llm, IPromptProvider prompts, ILogger<Questioner> logger)
    {
        _llm = llm;
        _prompts = prompts;
        _logger = logger;
    }

    public async Task<QuestionerResponse> AskAsync(QuestionerRequest request, CancellationToken ct = default)
    {
        // Stable prefix: role prompt + the probeable subgraph (constant within a round). The volatile part
        // (mastery so far, already-asked nodes, remaining budget) goes in the user turn.
        var system = _prompts.GetSystemPrompt(PromptRole, PromptVersion);
        var asked = request.AskedNodeIds.Length == 0 ? "(none yet)" : string.Join(", ", request.AskedNodeIds);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, system),
            new(ChatRole.System, $"## Skill subgraph (probeable nodes)\n\n{request.SubgraphJson}"),
            new(ChatRole.User,
                $"## Current mastery state\n\n{request.MasteryStateJson}\n\n" +
                $"## Already asked (do not repeat)\n\n{asked}\n\n" +
                $"## Questions left in budget\n\n{request.BudgetLeft}"),
        };

        _logger.LogInformation("Selecting next question (budget left {Budget}, {Asked} asked).",
            request.BudgetLeft, request.AskedNodeIds.Length);

        return await _llm.CompleteStructuredAsync<QuestionerResponse>(
            messages, PromptSchemas.QuestionerJsonSchema, ToolName, ct);
    }
}
