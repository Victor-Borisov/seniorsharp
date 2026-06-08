using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeniorSharp.Contracts;
using SeniorSharp.Domain;
using SeniorSharp.Llm;
using SeniorSharp.Persistence;

namespace SeniorSharp.Orchestration;

/// <summary>
/// Default orchestrator. Persists sessions/rounds/turns via <see cref="AppDbContext"/> and drives the full
/// MVP FSM: Discovery (conversational) -> DeepDive -> SystemDesign (both graph-driven) -> Scoring (scorer
/// ensemble -> Verdict) -> Report -> Done. <see cref="SubmitAnswerAsync"/> runs the within-round loop;
/// <see cref="AdvanceAsync"/> drives round transitions and scoring.
/// </summary>
public sealed class InterviewOrchestrator : IInterviewOrchestrator
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly string[] Axes = { "TechnicalDepth", "Architecture", "ProductionMaturity", "Communication" };

    private readonly AppDbContext _db;
    private readonly IQuestioner _questioner;
    private readonly IClassifier _classifier;
    private readonly IScorer _scorer;
    private readonly ILlmClient _llm;
    private readonly IPromptProvider _prompts;
    private readonly InterviewOptions _interview;
    private readonly AnthropicOptions _llmOptions;
    private readonly ILogger<InterviewOrchestrator> _logger;

    public InterviewOrchestrator(
        AppDbContext db,
        IQuestioner questioner,
        IClassifier classifier,
        IScorer scorer,
        ILlmClient llm,
        IPromptProvider prompts,
        IOptions<InterviewOptions> interview,
        IOptions<AnthropicOptions> llmOptions,
        ILogger<InterviewOrchestrator> logger)
    {
        _db = db;
        _questioner = questioner;
        _classifier = classifier;
        _scorer = scorer;
        _llm = llm;
        _prompts = prompts;
        _interview = interview.Value;
        _llmOptions = llmOptions.Value;
        _logger = logger;
    }

    public async Task<StartInterviewResult> StartAsync(StartInterviewRequest request, CancellationToken ct = default)
    {
        var graphVersion = request.GraphVersion
            ?? await _db.SkillNodes.Select(n => n.GraphVersion).FirstOrDefaultAsync(ct)
            ?? string.Empty;

        var session = new Session
        {
            Id = Guid.NewGuid(),
            CandidateRef = request.CandidateRef,
            State = InterviewState.Discovery,
            Status = SessionStatus.InProgress,
            StartedAt = DateTimeOffset.UtcNow,
            GraphVersion = graphVersion,
            PromptVersion = "v1",
            ModelId = _llmOptions.Model,
            Language = request.Language ?? string.Empty,
        };

        var round = new Round
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Type = RoundType.Discovery,
            Order = 0,
            Status = "open",
        };
        session.Rounds.Add(round);
        _db.Sessions.Add(session);

        var question = await AskDiscoveryQuestionAsync(round, Languages.NameOf(session.Language), ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Started session {SessionId} at Discovery.", session.Id);
        return new StartInterviewResult(session.Id, session.State, question);
    }

    public async Task<SubmitAnswerResult> SubmitAnswerAsync(SubmitAnswerRequest request, CancellationToken ct = default)
    {
        var session = await LoadSessionAsync(request.SessionId, ct);
        var round = ActiveRound(session)
            ?? throw new InvalidOperationException("No open round to answer.");

        AppendTurn(round, TurnRole.Candidate, request.AnswerText);

        // Graph rounds classify the answer against the probed node and update mastery; discovery just
        // collects transcript (the final scorer reads it for Communication / Production signals).
        if (round.Type is RoundType.DeepDive or RoundType.SystemDesign && !string.IsNullOrEmpty(round.PendingSkillId))
        {
            await ClassifyAndUpdateMasteryAsync(session, round, request.AnswerText, ct);
            round.PendingSkillId = null;
        }

        var asked = round.Turns.Count(t => t.Role == TurnRole.Interviewer);
        if (asked >= BudgetFor(round.Type))
        {
            round.Status = "complete";
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("{Round} round complete for session {SessionId} after {Asked} probes.",
                round.Type, session.Id, asked);
            return new SubmitAnswerResult(session.Id, session.State, NextQuestion: null, RoundComplete: true);
        }

        var nextQuestion = round.Type == RoundType.Discovery
            ? await AskDiscoveryQuestionAsync(round, Languages.NameOf(session.Language), ct)
            : await AskGraphQuestionAsync(session, round, ct);

        await _db.SaveChangesAsync(ct);
        return new SubmitAnswerResult(session.Id, session.State, nextQuestion, RoundComplete: false);
    }

    public async Task<AdvanceResult> AdvanceAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await LoadSessionAsync(sessionId, ct);

        if (InterviewStateMachine.IsTerminal(session.State))
            return new AdvanceResult(session.Id, session.State, IsTerminal: true);

        var next = InterviewStateMachine.Advance(session.State);
        session.State = next;

        // Moving into another interviewing round: open it and ask its first question.
        if (InterviewStateMachine.RoundFor(next) is { } roundType)
        {
            var round = new Round
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                Type = roundType,
                Order = session.Rounds.Count == 0 ? 0 : session.Rounds.Max(r => r.Order) + 1,
                Status = "open",
            };
            _db.Rounds.Add(round);
            session.Rounds.Add(round);

            var question = roundType == RoundType.Discovery
                ? await AskDiscoveryQuestionAsync(round, Languages.NameOf(session.Language), ct)
                : await AskGraphQuestionAsync(session, round, ct);

            await _db.SaveChangesAsync(ct);
            return new AdvanceResult(session.Id, session.State, IsTerminal: false, NextQuestion: question);
        }

        // Moving into Scoring: run the scorer ensemble, persist the verdict, then run to completion.
        if (next == InterviewState.Scoring)
        {
            session.Status = SessionStatus.Scoring;
            await RunScoringAsync(session, ct);
            // M3: the Report artifact is the persisted Verdict, so run Scoring -> Report -> Done in one step.
            session.State = InterviewState.Done;
            session.Status = SessionStatus.Completed;
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Session {SessionId} scored and completed.", session.Id);
            return new AdvanceResult(session.Id, session.State, IsTerminal: true);
        }

        await _db.SaveChangesAsync(ct);
        return new AdvanceResult(session.Id, session.State, InterviewStateMachine.IsTerminal(session.State));
    }

    // --- round questioning ----------------------------------------------

    private async Task<string> AskDiscoveryQuestionAsync(Round round, string language, CancellationToken ct)
    {
        var system = _prompts.GetSystemPrompt("discovery", "v1");
        var messages = new List<ChatMessage> { new(ChatRole.System, system) };
        if (!string.IsNullOrEmpty(language) && language != "English")
            messages.Add(new ChatMessage(ChatRole.System, $"Ask the question in {language}."));

        // Replay the round so far so the LLM builds on prior answers.
        foreach (var t in round.Turns.OrderBy(t => t.CreatedAt))
            messages.Add(new ChatMessage(
                t.Role == TurnRole.Interviewer ? ChatRole.Assistant : ChatRole.User, t.Content));
        if (round.Turns.Count == 0)
            messages.Add(new ChatMessage(ChatRole.User, "Begin the discovery round."));

        var question = (await _llm.CompleteTextAsync(messages, ct)).Trim();
        AppendTurn(round, TurnRole.Interviewer, question);
        return question;
    }

    private async Task<string> AskGraphQuestionAsync(Session session, Round round, CancellationToken ct)
    {
        var asked = round.Turns.Count(t => t.Role == TurnRole.Interviewer);
        var subgraph = await BuildSubgraphJsonAsync(session.GraphVersion, round.Type, ct);
        var askedIds = session.Mastery.Select(m => m.SkillId).Distinct().ToArray();

        var response = await _questioner.AskAsync(
            new QuestionerRequest(
                MasteryStateJson: BuildMasteryJson(session.Mastery),
                SubgraphJson: subgraph,
                AskedNodeIds: askedIds,
                BudgetLeft: BudgetFor(round.Type) - asked,
                Language: Languages.NameOf(session.Language)),
            ct);

        AppendTurn(round, TurnRole.Interviewer, response.QuestionText);
        round.PendingSkillId = response.NextSkillId;
        return response.QuestionText;
    }

    private async Task ClassifyAndUpdateMasteryAsync(Session session, Round round, string answer, CancellationToken ct)
    {
        var skillId = round.PendingSkillId!;
        var question = round.Turns.Where(t => t.Role == TurnRole.Interviewer).OrderBy(t => t.CreatedAt).Last().Content;
        var node = await _db.SkillNodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == skillId, ct);
        var answerTurn = round.Turns.Where(t => t.Role == TurnRole.Candidate).OrderBy(t => t.CreatedAt).Last();

        var classification = await _classifier.ClassifyAsync(
            new ClassifierRequest(
                NodeJson: node is null ? $"{{\"id\":\"{skillId}\"}}" : JsonSerializer.Serialize(ToNodeView(node), Json),
                Question: question,
                CandidateAnswer: answer,
                MasteryStateJson: BuildMasteryJson(session.Mastery),
                Language: Languages.NameOf(session.Language)),
            ct);

        var mastery = session.Mastery.FirstOrDefault(m => m.SkillId == skillId);
        if (mastery is null)
        {
            mastery = new SkillMastery { Id = Guid.NewGuid(), SessionId = session.Id, SkillId = skillId };
            _db.SkillMasteries.Add(mastery);   // explicit Add => Added; EF fixup fills session.Mastery
        }

        mastery.Recognition = classification.Recognition;
        mastery.Application = classification.Application;
        mastery.Depth = classification.Depth;
        if (!mastery.EvidenceTurnIds.Contains(answerTurn.Id))
            mastery.EvidenceTurnIds.Add(answerTurn.Id);
    }

    // --- scoring --------------------------------------------------------

    private async Task RunScoringAsync(Session session, CancellationToken ct)
    {
        var transcript = BuildTranscriptJson(session);
        var criteria = _prompts.GetCriteria();
        var runs = new List<ScorerResponse>();

        for (var i = 0; i < _interview.ScorerRuns; i++)
        {
            var verdict = await _scorer.ScoreAsync(
                new ScorerRequest(transcript, criteria, Axes, Languages.NameOf(session.Language)), ct);
            runs.Add(verdict);

            foreach (var axis in verdict.Axes)
            {
                _db.AxisScores.Add(new AxisScore
                {
                    Id = Guid.NewGuid(),
                    SessionId = session.Id,
                    Axis = Enum.TryParse<MasteryAxis>(axis.Axis, out var a) ? a : MasteryAxis.TechnicalDepth,
                    Level = axis.Level,
                    Score = axis.Score,
                    Rationale = axis.Rationale,
                    Citations = axis.Citations,
                    RunIndex = i,
                });
            }
        }

        // Aggregate: modal overall level, spread = stddev of each run's mean axis score, per-axis means.
        var overallLevel = runs
            .GroupBy(r => r.OverallLevel)
            .OrderByDescending(g => g.Count())
            .First().Key;

        var runMeans = runs.Select(r => r.Axes.Length == 0 ? 0 : r.Axes.Average(a => a.Score)).ToArray();
        var spread = StdDev(runMeans);

        var profile = Axes.Select(axis => new
        {
            axis,
            meanScore = runs
                .SelectMany(r => r.Axes)
                .Where(a => a.Axis == axis)
                .DefaultIfEmpty()
                .Average(a => a?.Score ?? 0),
        }).ToArray();

        _db.Verdicts.Add(new Verdict
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            OverallLevel = overallLevel,
            Summary = runs.First(r => r.OverallLevel == overallLevel).Summary,
            RunCount = runs.Count,
            Spread = spread,
            ProfileJson = JsonSerializer.Serialize(profile, Json),
        });

        _logger.LogInformation("Verdict for {SessionId}: {Level} (runs {N}, spread {Spread:F3}).",
            session.Id, overallLevel, runs.Count, spread);
    }

    // --- helpers --------------------------------------------------------

    private async Task<Session> LoadSessionAsync(Guid sessionId, CancellationToken ct) =>
        await _db.Sessions
            .Include(s => s.Rounds).ThenInclude(r => r.Turns)
            .Include(s => s.Mastery)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct)
        ?? throw new InvalidOperationException($"Session {sessionId} not found.");

    private static Round? ActiveRound(Session session) =>
        session.Rounds.Where(r => r.Status == "open").OrderBy(r => r.Order).LastOrDefault();

    private int BudgetFor(RoundType type) => type switch
    {
        RoundType.Discovery => _interview.DiscoveryBudget,
        RoundType.DeepDive => _interview.DeepDiveBudget,
        RoundType.SystemDesign => _interview.SystemDesignBudget,
        _ => _interview.DeepDiveBudget,
    };

    private Turn AppendTurn(Round round, TurnRole role, string content)
    {
        var turn = new Turn
        {
            Id = Guid.NewGuid(),
            RoundId = round.Id,
            Role = role,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Turns.Add(turn);   // explicit Add => Added; EF fixup fills round.Turns (no manual add → no dupe)
        return turn;
    }

    private async Task<string> BuildSubgraphJsonAsync(string graphVersion, RoundType roundType, CancellationToken ct)
    {
        var query = _db.SkillNodes.AsNoTracking().Where(n => n.GraphVersion == graphVersion);
        query = roundType == RoundType.SystemDesign
            ? query.Where(n => n.Layer == _interview.SystemDesignLayer)
            : query.Where(n => n.Layer != _interview.SystemDesignLayer);

        // Deterministic order so the serialized subgraph is byte-identical across the round's questioner
        // calls. Without an explicit OrderBy the DB row order is not guaranteed; if it shifts between calls
        // the cached system prefix changes and prompt caching silently misses (full price every call).
        var nodes = await query.OrderBy(n => n.Id).ToListAsync(ct);
        return JsonSerializer.Serialize(nodes.Select(ToQuestionerNodeView), Json);
    }

    /// <summary>Full node view — includes <c>exampleProbe</c>. Used by the classifier (single probed node).</summary>
    private static object ToNodeView(SkillNode n) => new
    {
        id = n.Id,
        title = n.Title,
        layer = n.Layer,
        axes = n.Axes,
        prerequisites = n.Prerequisites,
        seniorSignal = n.SeniorSignal,
        exampleProbe = n.ExampleProbe,
    };

    /// <summary>
    /// Lean node view for the questioner subgraph: everything needed to choose and phrase a probe, but
    /// WITHOUT <c>exampleProbe</c>. The questioner is instructed to invent its own mechanism question and
    /// never recite the probe, so shipping the probe for every node of the layer on every questioner call
    /// was dead weight (one of the two longest fields × the whole layer × every call).
    /// </summary>
    private static object ToQuestionerNodeView(SkillNode n) => new
    {
        id = n.Id,
        title = n.Title,
        layer = n.Layer,
        axes = n.Axes,
        prerequisites = n.Prerequisites,
        seniorSignal = n.SeniorSignal,
    };

    private static string BuildMasteryJson(IEnumerable<SkillMastery> mastery) =>
        JsonSerializer.Serialize(
            mastery.Select(m => new { skillId = m.SkillId, recognition = m.Recognition, application = m.Application, depth = m.Depth }),
            Json);

    private static string BuildTranscriptJson(Session session) =>
        JsonSerializer.Serialize(new
        {
            rounds = session.Rounds.OrderBy(r => r.Order).Select(r => new
            {
                round = r.Type.ToString(),
                turns = r.Turns.OrderBy(t => t.CreatedAt).Select(t => new { role = t.Role.ToString(), content = t.Content }),
            }),
        }, Json);

    private static double StdDev(IReadOnlyCollection<double> values)
    {
        if (values.Count < 2) return 0;
        var mean = values.Average();
        return Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / values.Count);
    }
}
