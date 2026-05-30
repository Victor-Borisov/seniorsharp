using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SeniorSharp.Contracts;
using SeniorSharp.Llm;
using SeniorSharp.Orchestration;

namespace SeniorSharp.Tests;

/// <summary>Captures what the orchestration layer passes to the LLM and returns a canned result.</summary>
internal sealed class FakeLlmClient : ILlmClient
{
    public IReadOnlyList<ChatMessage>? CapturedMessages { get; private set; }
    public string? CapturedSchema { get; private set; }
    public string? CapturedToolName { get; private set; }
    public object? StructuredResult { get; set; }

    public Task<TResult> CompleteStructuredAsync<TResult>(
        IReadOnlyList<ChatMessage> messages, string jsonSchema, string toolName, CancellationToken ct = default)
    {
        CapturedMessages = messages;
        CapturedSchema = jsonSchema;
        CapturedToolName = toolName;
        return Task.FromResult((TResult)StructuredResult!);
    }

    public Task<string> CompleteTextAsync(
        IReadOnlyList<ChatMessage> messages, CancellationToken ct = default, string? modelOverride = null)
        => Task.FromResult("Tell me about a system you owned.");
}

internal sealed class FakePromptProvider : IPromptProvider
{
    public string Prompt { get; set; } = "SYSTEM_PROMPT";

    public string GetSystemPrompt(string role, string version) => Prompt;

    public string GetCriteria() => "CRITERIA";
}

/// <summary>Returns the supplied skill ids in order, one per call.</summary>
internal sealed class FakeQuestioner : IQuestioner
{
    private readonly string[] _skillIds;
    private int _i;

    public FakeQuestioner(params string[] skillIds) => _skillIds = skillIds;

    public Task<QuestionerResponse> AskAsync(QuestionerRequest request, System.Threading.CancellationToken ct = default)
    {
        var id = _skillIds[_i++ % _skillIds.Length];
        return Task.FromResult(new QuestionerResponse(id, $"Question about {id}?", "because", "TechnicalDepth"));
    }
}

internal sealed class FakeClassifier : IClassifier
{
    public Task<ClassifierResponse> ClassifyAsync(ClassifierRequest request, System.Threading.CancellationToken ct = default)
        => Task.FromResult(new ClassifierResponse(0.8, 0.7, 0.6, "quote", System.Array.Empty<string>()));
}

internal sealed class FakeCandidate : ICandidate
{
    public Task<string> AnswerAsync(string question, string nodeContext, string level, CancellationToken ct = default)
        => Task.FromResult($"A {level}-level answer to: {question}");
}

internal sealed class FakeScorer : IScorer
{
    public Task<ScorerResponse> ScoreAsync(ScorerRequest request, System.Threading.CancellationToken ct = default)
        => Task.FromResult(new ScorerResponse(
            request.Axes.Select(a => new AxisScoreDto(a, "Senior", 0.85, "rationale", new[] { "cite" })).ToArray(),
            "Senior",
            "Strong senior signals."));
}
