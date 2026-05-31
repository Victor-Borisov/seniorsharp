using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeniorSharp.Contracts;
using SeniorSharp.Llm;

namespace SeniorSharp.Orchestration;

/// <summary>
/// LLM-backed classifier. Forces structured tool-use against
/// <see cref="PromptSchemas.ClassifierJsonSchema"/> to grade one answer on
/// recognition/application/depth.
/// </summary>
public sealed class Classifier : IClassifier
{
    private const string ToolName = "emit_classification";
    private const string PromptRole = "classifier";
    private const string PromptVersion = "v1";

    private readonly ILlmClient _llm;
    private readonly IPromptProvider _prompts;
    private readonly ILogger<Classifier> _logger;

    public Classifier(ILlmClient llm, IPromptProvider prompts, ILogger<Classifier> logger)
    {
        _llm = llm;
        _prompts = prompts;
        _logger = logger;
    }

    public async Task<ClassifierResponse> ClassifyAsync(ClassifierRequest request, CancellationToken ct = default)
    {
        var system = _prompts.GetSystemPrompt(PromptRole, PromptVersion);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, system),
            new(ChatRole.System, $"## Skill node under assessment\n\n{request.NodeJson}"),
            new(ChatRole.User,
                $"## Question asked\n\n{request.Question}\n\n" +
                $"## Candidate answer\n\n{request.CandidateAnswer}\n\n" +
                $"## Prior mastery (for calibration)\n\n{request.MasteryStateJson}"),
        };

        if (!string.IsNullOrEmpty(request.Language))
            messages.Add(new ChatMessage(ChatRole.System,
                $"Write any prose in {request.Language}. The evidenceQuote must stay verbatim from the answer."));

        _logger.LogInformation("Classifying answer ({Length} chars).", request.CandidateAnswer.Length);

        return await _llm.CompleteStructuredAsync<ClassifierResponse>(
            messages, PromptSchemas.ClassifierJsonSchema, ToolName, ct);
    }
}
