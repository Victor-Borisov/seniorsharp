namespace SeniorSharp.Domain;

/// <summary>
/// A single utterance within a round. Turns are append-only and immutable once created:
/// the transcript is never edited, only appended to.
/// </summary>
public class Turn
{
    public Guid Id { get; set; }

    public Guid RoundId { get; set; }

    public TurnRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
