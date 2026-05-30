using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeniorSharp.Llm;

namespace SeniorSharp.Orchestration;

/// <summary>
/// <see cref="ICandidate"/> that role-plays a developer of a target level via the LLM (plain text
/// completion). Test/eval scaffolding — never part of a real candidate's interview.
/// </summary>
public sealed class SimulatedCandidate : ICandidate
{
    private const string PromptRole = "candidate-sim";
    private const string PromptVersion = "v1";

    private readonly ILlmClient _llm;
    private readonly IPromptProvider _prompts;
    private readonly InterviewOptions _interview;
    private readonly ILogger<SimulatedCandidate> _logger;

    public SimulatedCandidate(
        ILlmClient llm, IPromptProvider prompts, IOptions<InterviewOptions> interview,
        ILogger<SimulatedCandidate> logger)
    {
        _llm = llm;
        _prompts = prompts;
        _interview = interview.Value;
        _logger = logger;
    }

    public async Task<string> AnswerAsync(
        string question, string nodeContext, string level, CancellationToken ct = default)
    {
        var persona = _prompts.GetSystemPrompt(PromptRole, PromptVersion);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, persona),
            new(ChatRole.System, $"You are playing a **{level}**-level .NET developer. Answer in character for that level."),
            new(ChatRole.System, $"## Topic context (for realism; do not quote verbatim)\n\n{nodeContext}"),
            new(ChatRole.User, question),
        };

        // Senior/expert use the strong default model; weaker levels use a cheaper model so the answers are
        // genuinely sub-senior (a strong model role-playing "middle" leaks too much competence — see M4).
        var isStrong = level.Equals("senior", System.StringComparison.OrdinalIgnoreCase)
                       || level.Equals("expert", System.StringComparison.OrdinalIgnoreCase);
        var model = isStrong ? null : _interview.CandidateModel;

        _logger.LogInformation("Simulated {Level} candidate answering (model {Model}).", level, model ?? "default");

        return await _llm.CompleteTextAsync(messages, ct, model);
    }
}
