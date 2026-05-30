using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SeniorSharp.Persistence;

namespace SeniorSharp.Orchestration;

/// <summary>
/// Drives a full interview end-to-end using a <see cref="ICandidate"/> (no human). Test/eval scaffolding:
/// backs the interview demo and the M4 autoeval (synthetic profiles of a known level). Shares the scoped
/// <see cref="AppDbContext"/> with the orchestrator resolved from the same scope.
/// </summary>
public sealed class InterviewSimulator
{
    private readonly IInterviewOrchestrator _orchestrator;
    private readonly ICandidate _candidate;
    private readonly AppDbContext _db;
    private readonly ILogger<InterviewSimulator> _logger;

    public InterviewSimulator(
        IInterviewOrchestrator orchestrator,
        ICandidate candidate,
        AppDbContext db,
        ILogger<InterviewSimulator> logger)
    {
        _orchestrator = orchestrator;
        _candidate = candidate;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Runs the whole FSM (discovery -> deep-dive -> system-design -> scoring) with a simulated candidate
    /// of <paramref name="level"/>, and returns the session id (its Verdict is persisted).
    /// <paramref name="onTurn"/>, if supplied, is invoked with each (question, answer) pair.
    /// </summary>
    public async Task<Guid> RunAsync(
        string candidateRef,
        string level,
        Action<string, string>? onTurn = null,
        CancellationToken ct = default)
    {
        var start = await _orchestrator.StartAsync(new StartInterviewRequest(candidateRef), ct);
        var sessionId = start.SessionId;
        var question = start.FirstQuestion;

        var guard = 0;
        while (question is not null)
        {
            if (++guard > 50)
                throw new InvalidOperationException("Interview did not terminate within the turn guard.");

            var nodeCtx = await PendingNodeContextAsync(sessionId, ct);
            var answer = await _candidate.AnswerAsync(question, nodeCtx, level, ct);
            onTurn?.Invoke(question, answer);

            var res = await _orchestrator.SubmitAnswerAsync(new SubmitAnswerRequest(sessionId, answer), ct);
            if (!res.RoundComplete)
            {
                question = res.NextQuestion;
                continue;
            }

            var adv = await _orchestrator.AdvanceAsync(sessionId, ct);
            question = adv.NextQuestion; // null => moved into scoring/done
        }

        _logger.LogInformation("Simulated {Level} interview {SessionId} complete.", level, sessionId);
        return sessionId;
    }

    private async Task<string> PendingNodeContextAsync(Guid sessionId, CancellationToken ct)
    {
        var round = await _db.Rounds.AsNoTracking()
            .Where(r => r.SessionId == sessionId && r.Status == "open")
            .OrderBy(r => r.Order).LastOrDefaultAsync(ct);

        if (round?.PendingSkillId is not { } sk)
            return string.Empty;

        var node = await _db.SkillNodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == sk, ct);
        return node is null ? string.Empty : $"{node.Title}: {node.Description}\nSenior signal: {node.SeniorSignal}";
    }
}
