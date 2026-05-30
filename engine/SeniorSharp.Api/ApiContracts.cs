namespace SeniorSharp.Api;

/// <summary>Response for GET /health.</summary>
public sealed record HealthResponse(string Status);

/// <summary>Body for POST /sessions.</summary>
public sealed record CreateSessionRequest(string? CandidateRef, string? GraphVersion);

/// <summary>Response for POST /sessions (creation + first question).</summary>
public sealed record SessionResponse(Guid Id, string State, string FirstQuestion);

/// <summary>Response for GET /sessions/{id}.</summary>
public sealed record SessionDetailResponse(
    Guid Id,
    string State,
    string Status,
    DateTimeOffset StartedAt,
    string GraphVersion,
    string PromptVersion,
    string ModelId);

/// <summary>Body for POST /sessions/{id}/answer.</summary>
public sealed record SubmitAnswerBody(string Answer);

/// <summary>Response for POST /sessions/{id}/answer.</summary>
public sealed record AnswerResponse(
    Guid SessionId,
    string State,
    string? NextQuestion,
    bool RoundComplete);

/// <summary>Body for POST /voice/sessions (managed voice provider starts a call).</summary>
public sealed record VoiceStartBody(string? CandidateRef);

/// <summary>Body for POST /voice/sessions/{id}/turn (a transcribed candidate utterance).</summary>
public sealed record VoiceTurnBody(string Utterance);

/// <summary>Response for the voice endpoints: the next thing to speak + whether the interview is over.</summary>
public sealed record VoiceTurnResponse(Guid SessionId, string Utterance, bool IsComplete);

/// <summary>One axis line in the report (modal level + mean score across scorer runs).</summary>
public sealed record AxisVerdictDto(string Axis, string Level, double Score, string Rationale, string[] Citations);

/// <summary>Response for GET /sessions/{id}/verdict — the report dashboard payload.</summary>
public sealed record VerdictResponse(
    Guid SessionId,
    string OverallLevel,
    string Summary,
    int RunCount,
    double Spread,
    AxisVerdictDto[] Axes);
