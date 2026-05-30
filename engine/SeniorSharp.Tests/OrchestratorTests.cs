using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SeniorSharp.Domain;
using SeniorSharp.Llm;
using SeniorSharp.Orchestration;
using SeniorSharp.Persistence;
using Xunit;

namespace SeniorSharp.Tests;

// SQLite in-memory mirrors real relational behavior (change tracking, FKs) far more faithfully than the
// EF InMemory provider; the connection is kept open for the test so the database persists across contexts.
public sealed class OrchestratorTests : IDisposable
{
    private const string GraphVersion = "v-test";
    private const string ArchLayer = "Architecture & system design";

    private readonly SqliteConnection _conn;

    // Stateful questioner shared across the per-call contexts so it keeps advancing (n1 -> n2 -> a1).
    private readonly FakeQuestioner _questioner = new("n1", "n2", "a1");
    private readonly FakeClassifier _classifier = new();
    private readonly FakeScorer _scorer = new();

    public OrchestratorTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        using var db = Db();
        db.Database.EnsureCreated();
        db.SkillNodes.AddRange(
            MakeNode("n1", "C# language"),
            MakeNode("n2", "C# language"),
            MakeNode("a1", ArchLayer));
        db.SaveChanges();
    }

    public void Dispose() => _conn.Dispose();

    private AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options);

    private static SkillNode MakeNode(string id, string layer) => new()
    {
        Id = id,
        Title = $"Node {id}",
        Layer = layer,
        Description = "desc",
        Axes = new[] { "TechnicalDepth" },
        Prerequisites = Array.Empty<string>(),
        MasteryFocus = Array.Empty<string>(),
        SeniorSignal = "signal",
        ExampleProbe = "probe",
        GraphVersion = GraphVersion,
    };

    private InterviewOrchestrator NewOrchestrator(AppDbContext db) => new(
        db, _questioner, _classifier, _scorer,
        new FakeLlmClient(), new FakePromptProvider(),
        Options.Create(new InterviewOptions
        {
            DiscoveryBudget = 1,
            DeepDiveBudget = 2,
            SystemDesignBudget = 1,
            SystemDesignLayer = ArchLayer,
            ScorerRuns = 2,
        }),
        Options.Create(new AnthropicOptions { Model = "test-model" }),
        NullLogger<InterviewOrchestrator>.Instance);

    [Fact]
    public async Task Full_interview_flows_through_all_rounds_to_a_persisted_verdict()
    {
        Guid sessionId;
        string? question;
        using (var db = Db())
        {
            var start = await NewOrchestrator(db).StartAsync(new StartInterviewRequest("cand-1"));
            sessionId = start.SessionId;
            question = start.FirstQuestion;
            Assert.Equal(InterviewState.Discovery, start.State);
        }

        var guard = 0;
        while (question is not null)
        {
            Assert.True(++guard <= 20, "interview did not terminate");
            using var db = Db();
            var orch = NewOrchestrator(db);
            var res = await orch.SubmitAnswerAsync(new SubmitAnswerRequest(sessionId, "an answer"));
            if (!res.RoundComplete)
            {
                question = res.NextQuestion;
                continue;
            }
            var adv = await orch.AdvanceAsync(sessionId);
            question = adv.NextQuestion;
        }

        using var check = Db();
        var session = await check.Sessions.FirstAsync(s => s.Id == sessionId);
        Assert.Equal(InterviewState.Done, session.State);
        Assert.Equal(SessionStatus.Completed, session.Status);

        // All three MVP rounds were created.
        var roundTypes = await check.Rounds.Where(r => r.SessionId == sessionId).Select(r => r.Type).ToListAsync();
        Assert.Contains(RoundType.Discovery, roundTypes);
        Assert.Contains(RoundType.DeepDive, roundTypes);
        Assert.Contains(RoundType.SystemDesign, roundTypes);

        // Verdict + ensemble axis scores persisted (ScorerRuns=2 × 4 axes).
        var verdict = await check.Verdicts.FirstOrDefaultAsync(v => v.SessionId == sessionId);
        Assert.NotNull(verdict);
        Assert.Equal("Senior", verdict!.OverallLevel);
        Assert.Equal(2, verdict.RunCount);
        Assert.Equal(8, await check.AxisScores.CountAsync(a => a.SessionId == sessionId));

        // Graph rounds produced mastery (deep-dive n1/n2 + system-design a1); discovery adds none.
        var mastery = await check.SkillMasteries.Where(m => m.SessionId == sessionId).Select(m => m.SkillId).ToListAsync();
        Assert.Contains("a1", mastery);
        Assert.True(mastery.Count >= 2);
    }

    [Fact]
    public async Task Simulator_runs_a_full_interview_and_persists_a_verdict()
    {
        Guid sessionId;
        using (var db = Db())
        {
            var sim = new InterviewSimulator(
                NewOrchestrator(db), new FakeCandidate(), db, NullLogger<InterviewSimulator>.Instance);
            sessionId = await sim.RunAsync("eval-senior", "senior");
        }

        using var check = Db();
        var session = await check.Sessions.FirstAsync(s => s.Id == sessionId);
        Assert.Equal(InterviewState.Done, session.State);
        Assert.NotNull(await check.Verdicts.FirstOrDefaultAsync(v => v.SessionId == sessionId));
        Assert.Equal(8, await check.AxisScores.CountAsync(a => a.SessionId == sessionId));
    }

    [Fact]
    public async Task Start_opens_a_discovery_round_with_a_first_question()
    {
        Guid sessionId;
        using (var db = Db())
            sessionId = (await NewOrchestrator(db).StartAsync(new StartInterviewRequest("cand-2"))).SessionId;

        using var check = Db();
        var round = await check.Rounds.Include(r => r.Turns).FirstAsync(r => r.SessionId == sessionId);
        Assert.Equal(RoundType.Discovery, round.Type);
        Assert.Null(round.PendingSkillId);   // discovery is conversational, no probed node
        Assert.Single(round.Turns);
        Assert.Equal(TurnRole.Interviewer, round.Turns[0].Role);
    }
}
