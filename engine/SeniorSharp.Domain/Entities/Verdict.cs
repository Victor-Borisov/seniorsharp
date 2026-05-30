namespace SeniorSharp.Domain;

/// <summary>
/// Final aggregated verdict for a session across all scorer runs.
/// </summary>
public class Verdict
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public string OverallLevel { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    /// <summary>Number of scorer runs aggregated into this verdict.</summary>
    public int RunCount { get; set; }

    /// <summary>Spread/variance across runs, used as a confidence signal.</summary>
    public double Spread { get; set; }

    /// <summary>Serialized full profile (per-axis breakdown) as JSON.</summary>
    public string ProfileJson { get; set; } = string.Empty;
}
