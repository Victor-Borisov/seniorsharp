namespace SeniorSharp.Llm;

/// <summary>
/// A single chat message in a conversation with the LLM.
/// </summary>
public record ChatMessage(ChatRole Role, string Content);
