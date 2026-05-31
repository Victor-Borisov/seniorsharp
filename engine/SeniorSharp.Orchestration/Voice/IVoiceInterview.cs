using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeniorSharp.Orchestration;

/// <summary>
/// Turn-oriented facade over the interview FSM for a managed voice provider (Retell / Vapi / OpenAI
/// Realtime). The provider handles STT/TTS/turn-taking; it only needs "here is what the candidate said,
/// what do I say next?". This adapter hides rounds, FSM transitions and end-of-interview scoring behind a
/// single <see cref="NextTurnAsync"/> call. The verdict is always computed from the transcript, so voice
/// and text share one engine.
/// </summary>
public interface IVoiceInterview
{
    /// <summary>Starts a session (in the given language locale) and returns the first utterance to speak.</summary>
    Task<VoiceStartResult> StartAsync(string? candidateRef, string? language = null, CancellationToken ct = default);

    /// <summary>
    /// Records the candidate's (transcribed) utterance and returns the next utterance to speak, advancing
    /// rounds and running scoring as needed. When the interview is over, <see cref="VoiceTurnResult.IsComplete"/>
    /// is true and the utterance is a closing remark (the verdict is already persisted).
    /// </summary>
    Task<VoiceTurnResult> NextTurnAsync(Guid sessionId, string candidateUtterance, CancellationToken ct = default);
}

/// <summary>Result of starting a voice session.</summary>
public sealed record VoiceStartResult(Guid SessionId, string Utterance);

/// <summary>Result of one voice turn.</summary>
public sealed record VoiceTurnResult(Guid SessionId, string Utterance, bool IsComplete);
