namespace SeniorSharp.Contracts;

/// <summary>
/// JSON Schema strings for forced tool-use (structured output) for each LLM role.
/// Each schema corresponds exactly to the matching Response DTO. These strings are
/// passed as the <c>input_schema</c> of a single forced tool so the model is
/// constrained to emit a payload that deserializes into the Response record.
/// </summary>
public static class PromptSchemas
{
    /// <summary>JSON Schema for <see cref="QuestionerResponse"/>.</summary>
    public const string QuestionerJsonSchema = """
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["nextSkillId", "questionText", "rationale", "targetsAxis"],
          "properties": {
            "nextSkillId": {
              "type": "string",
              "description": "Id of the skill node selected to probe next."
            },
            "questionText": {
              "type": "string",
              "description": "The interviewer question text to present to the candidate."
            },
            "rationale": {
              "type": "string",
              "description": "Why this skill/question was chosen given the mastery state."
            },
            "targetsAxis": {
              "type": "string",
              "description": "Mastery axis this question primarily targets.",
              "enum": ["TechnicalDepth", "Architecture", "ProductionMaturity", "Communication"]
            }
          }
        }
        """;

    /// <summary>JSON Schema for <see cref="ClassifierResponse"/>.</summary>
    public const string ClassifierJsonSchema = """
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["recognition", "application", "depth", "evidenceQuote", "flags"],
          "properties": {
            "recognition": {
              "type": "number",
              "minimum": 0,
              "maximum": 1,
              "description": "Degree to which the candidate recognizes the concept (0..1)."
            },
            "application": {
              "type": "number",
              "minimum": 0,
              "maximum": 1,
              "description": "Degree to which the candidate can apply the concept (0..1)."
            },
            "depth": {
              "type": "number",
              "minimum": 0,
              "maximum": 1,
              "description": "Depth of understanding demonstrated (0..1)."
            },
            "evidenceQuote": {
              "type": "string",
              "description": "Verbatim quote from the answer supporting the scores."
            },
            "flags": {
              "type": "array",
              "items": { "type": "string" },
              "description": "Notable signals (red flags, strong signals) detected."
            }
          }
        }
        """;

    /// <summary>JSON Schema for <see cref="ScorerResponse"/> (contains <see cref="AxisScoreDto"/>).</summary>
    public const string ScorerJsonSchema = """
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["axes", "overallLevel", "summary"],
          "properties": {
            "axes": {
              "type": "array",
              "description": "Per-axis score breakdown.",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["axis", "level", "score", "rationale", "citations"],
                "properties": {
                  "axis": {
                    "type": "string",
                    "description": "Mastery axis name.",
                    "enum": ["TechnicalDepth", "Architecture", "ProductionMaturity", "Communication"]
                  },
                  "level": {
                    "type": "string",
                    "description": "Level assigned for this axis."
                  },
                  "score": {
                    "type": "number",
                    "minimum": 0,
                    "maximum": 1,
                    "description": "Numeric score for this axis (0..1)."
                  },
                  "rationale": {
                    "type": "string",
                    "description": "Explanation for the assigned level/score."
                  },
                  "citations": {
                    "type": "array",
                    "items": { "type": "string" },
                    "description": "Transcript citations supporting the score."
                  }
                }
              }
            },
            "overallLevel": {
              "type": "string",
              "description": "Overall seniority level verdict."
            },
            "summary": {
              "type": "string",
              "description": "Human-readable summary justifying the verdict."
            }
          }
        }
        """;
}
