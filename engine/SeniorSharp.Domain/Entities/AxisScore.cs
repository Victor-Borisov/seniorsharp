namespace SeniorSharp.Domain;

/// <summary>
/// Score for one mastery axis produced by a single scorer run.
/// </summary>
public class AxisScore
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public MasteryAxis Axis { get; set; }

    public string Level { get; set; } = string.Empty;

    public double Score { get; set; }

    public string Rationale { get; set; } = string.Empty;

    /// <summary>Quoted citations from the transcript supporting the score.</summary>
    public string[] Citations { get; set; } = [];

    /// <summary>Index of the scorer run (for multi-run spread analysis).</summary>
    public int RunIndex { get; set; }
}
