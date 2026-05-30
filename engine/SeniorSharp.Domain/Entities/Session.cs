namespace SeniorSharp.Domain;

/// <summary>
/// Aggregate root for a single interview session.
/// </summary>
public class Session
{
    public Guid Id { get; set; }

    /// <summary>Optional external reference to the candidate.</summary>
    public string? CandidateRef { get; set; }

    public InterviewState State { get; set; }

    public SessionStatus Status { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public string GraphVersion { get; set; } = string.Empty;

    public string PromptVersion { get; set; } = string.Empty;

    public string ModelId { get; set; } = string.Empty;

    public List<Round> Rounds { get; set; } = [];

    public List<SkillMastery> Mastery { get; set; } = [];
}
