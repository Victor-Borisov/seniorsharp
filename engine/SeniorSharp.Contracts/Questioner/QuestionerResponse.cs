namespace SeniorSharp.Contracts;

/// <summary>
/// Structured response from the questioner role describing the next question to ask.
/// Shape MUST stay in sync with <see cref="PromptSchemas.QuestionerJsonSchema"/>.
/// </summary>
/// <param name="NextSkillId">Id of the skill node selected to probe next.</param>
/// <param name="QuestionText">The interviewer question text to present to the candidate.</param>
/// <param name="Rationale">Why this skill/question was chosen given the mastery state.</param>
/// <param name="TargetsAxis">Mastery axis this question primarily targets.</param>
public sealed record QuestionerResponse(
    string NextSkillId,
    string QuestionText,
    string Rationale,
    string TargetsAxis);
