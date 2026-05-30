namespace SeniorSharp.Domain;

/// <summary>
/// A round within a session (Discovery / DeepDive / SystemDesign).
/// </summary>
public class Round
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public RoundType Type { get; set; }

    /// <summary>Ordinal position of the round inside the session.</summary>
    public int Order { get; set; }

    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Skill node id the currently-open interviewer question targets, set when a question is asked and
    /// cleared once the answer is classified. Persisted so an interrupted interview is resumable.
    /// </summary>
    public string? PendingSkillId { get; set; }

    public List<Turn> Turns { get; set; } = [];
}
