using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SeniorSharp.Contracts;
using SeniorSharp.Llm;
using SeniorSharp.Orchestration;
using Xunit;

namespace SeniorSharp.Tests;

public sealed class ScorerTests
{
    private static ScorerResponse SampleVerdict() => new(
        Axes: new[] { new AxisScoreDto("TechnicalDepth", "Senior", 0.9, "deep", new[] { "turn-1" }) },
        OverallLevel: "Senior",
        Summary: "Strong senior signals.");

    [Fact]
    public async Task ScoreAsync_forces_the_emit_score_tool_with_the_scorer_schema()
    {
        var llm = new FakeLlmClient { StructuredResult = SampleVerdict() };
        var scorer = new Scorer(llm, new FakePromptProvider(), NullLogger<Scorer>.Instance);

        await scorer.ScoreAsync(new ScorerRequest("T", "C", new[] { "TechnicalDepth" }));

        Assert.Equal("emit_score", llm.CapturedToolName);
        Assert.Equal(PromptSchemas.ScorerJsonSchema, llm.CapturedSchema);
    }

    [Fact]
    public async Task ScoreAsync_assembles_stable_system_prefix_and_transcript_user_turn()
    {
        var llm = new FakeLlmClient { StructuredResult = SampleVerdict() };
        var scorer = new Scorer(llm, new FakePromptProvider { Prompt = "SCORER_ROLE" }, NullLogger<Scorer>.Instance);

        await scorer.ScoreAsync(new ScorerRequest(
            TranscriptJson: "TRANSCRIPT_BODY",
            CriteriaJson: "CRITERIA_BODY",
            Axes: new[] { "TechnicalDepth", "Architecture" }));

        var messages = llm.CapturedMessages!;
        var systemText = string.Join("\n", messages.Where(m => m.Role == ChatRole.System).Select(m => m.Content));
        var userText = string.Join("\n", messages.Where(m => m.Role == ChatRole.User).Select(m => m.Content));

        // Stable, cacheable prefix carries role prompt + criteria + axes; transcript is the volatile user turn.
        Assert.Contains("SCORER_ROLE", systemText);
        Assert.Contains("CRITERIA_BODY", systemText);
        Assert.Contains("TechnicalDepth", systemText);
        Assert.Contains("Architecture", systemText);
        Assert.Contains("TRANSCRIPT_BODY", userText);
        Assert.DoesNotContain("TRANSCRIPT_BODY", systemText);
    }

    [Fact]
    public async Task ScoreAsync_returns_the_llm_verdict()
    {
        var llm = new FakeLlmClient { StructuredResult = SampleVerdict() };
        var scorer = new Scorer(llm, new FakePromptProvider(), NullLogger<Scorer>.Instance);

        var result = await scorer.ScoreAsync(new ScorerRequest("T", "C", new[] { "TechnicalDepth" }));

        Assert.Equal("Senior", result.OverallLevel);
        Assert.Single(result.Axes);
    }

    [Fact]
    public void ScorerResponse_deserializes_from_the_schema_camelCase_tool_input()
    {
        // Mirrors how AnthropicLlmClient maps a forced tool-use input (camelCase per ScorerJsonSchema)
        // back onto the PascalCase ScorerResponse record. This is the contract the live client relies on.
        const string toolInput = """
            {
              "axes": [
                { "axis": "TechnicalDepth", "level": "Senior", "score": 0.92,
                  "rationale": "Explains async state machine boxing.", "citations": ["deep-dive turn 1"] }
              ],
              "overallLevel": "Senior",
              "summary": "Consistently senior."
            }
            """;
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };

        var verdict = JsonSerializer.Deserialize<ScorerResponse>(toolInput, options);

        Assert.NotNull(verdict);
        Assert.Equal("Senior", verdict!.OverallLevel);
        Assert.Equal("TechnicalDepth", verdict.Axes[0].Axis);
        Assert.Equal(0.92, verdict.Axes[0].Score, 3);
        Assert.Equal("deep-dive turn 1", verdict.Axes[0].Citations[0]);
    }
}
