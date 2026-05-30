namespace SeniorSharp.Domain;

/// <summary>
/// Accumulated mastery signal for a single skill within a session.
/// </summary>
public class SkillMastery
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    /// <summary>Reference to <see cref="SkillNode.Id"/>.</summary>
    public string SkillId { get; set; } = string.Empty;

    public double Recognition { get; set; }

    public double Application { get; set; }

    public double Depth { get; set; }

    /// <summary>Ids of the turns that provide evidence for this mastery estimate.</summary>
    public List<Guid> EvidenceTurnIds { get; set; } = [];
}
