namespace SeniorSharp.Contracts;

/// <summary>
/// Structured response from the classifier role grading a candidate answer.
/// Shape MUST stay in sync with <see cref="PromptSchemas.ClassifierJsonSchema"/>.
/// </summary>
/// <param name="Recognition">Degree to which the candidate recognizes the concept (0..1).</param>
/// <param name="Application">Degree to which the candidate can apply it (0..1).</param>
/// <param name="Depth">Depth of understanding demonstrated (0..1).</param>
/// <param name="EvidenceQuote">Verbatim quote from the answer supporting the scores.</param>
/// <param name="Flags">Notable signals (e.g. red flags, strong signals) detected.</param>
public sealed record ClassifierResponse(
    double Recognition,
    double Application,
    double Depth,
    string EvidenceQuote,
    string[] Flags);
