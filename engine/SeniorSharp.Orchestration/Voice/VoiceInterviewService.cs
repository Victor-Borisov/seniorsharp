using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SeniorSharp.Orchestration;

/// <summary>
/// Default <see cref="IVoiceInterview"/>: wraps <see cref="IInterviewOrchestrator"/> so a voice provider
/// drives the whole interview through one utterance-in / utterance-out call.
/// </summary>
public sealed class VoiceInterviewService : IVoiceInterview
{
    // Spoken closing line when the interview ends (provider TTS's it, then hangs up). Language handling
    // (matching the candidate's language) lands with the UI work; English placeholder for now.
    private const string ClosingRemark =
        "Thank you — that concludes the interview. Your responses have been recorded and will be assessed.";

    private readonly IInterviewOrchestrator _orchestrator;
    private readonly ILogger<VoiceInterviewService> _logger;

    public VoiceInterviewService(IInterviewOrchestrator orchestrator, ILogger<VoiceInterviewService> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<VoiceStartResult> StartAsync(string? candidateRef, CancellationToken ct = default)
    {
        var start = await _orchestrator.StartAsync(new StartInterviewRequest(candidateRef), ct);
        return new VoiceStartResult(start.SessionId, start.FirstQuestion);
    }

    public async Task<VoiceTurnResult> NextTurnAsync(
        Guid sessionId, string candidateUtterance, CancellationToken ct = default)
    {
        var res = await _orchestrator.SubmitAnswerAsync(new SubmitAnswerRequest(sessionId, candidateUtterance), ct);

        // Still inside the current round — just ask the next question.
        if (!res.RoundComplete)
            return new VoiceTurnResult(sessionId, res.NextQuestion ?? string.Empty, IsComplete: false);

        // Round boundary: advance the FSM. This either opens the next round (returns its first question)
        // or moves into scoring (runs the ensemble + persists the verdict) and completes the interview.
        var adv = await _orchestrator.AdvanceAsync(sessionId, ct);
        if (adv.NextQuestion is { } next)
            return new VoiceTurnResult(sessionId, next, IsComplete: false);

        _logger.LogInformation("Voice interview {SessionId} complete (state {State}).", sessionId, adv.State);
        return new VoiceTurnResult(sessionId, ClosingRemark, IsComplete: true);
    }
}
