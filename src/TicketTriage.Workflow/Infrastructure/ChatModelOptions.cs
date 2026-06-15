namespace TicketTriage.Workflow.Infrastructure;

/// <summary>
/// Configuration for the OpenAI Chat Completions-compatible model used by every
/// agent in this workflow (Azure OpenAI, Groq, GitHub Models, etc. - see
/// AGENTS.md §3: "Azure OpenAI-compatible models ... or compatible clients").
/// Bound from the "ChatModel" section of appsettings.json, environment
/// variables, or user secrets - never hardcoded.
/// </summary>
public sealed class ChatModelOptions
{
    public const string SectionName = "ChatModel";

    /// <summary>Base URL of the OpenAI-compatible API, e.g. https://api.groq.com/openai/v1</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>API key for the chat model provider. Supply via user secrets or env var, never commit.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Name/id of the chat model to use (e.g. "llama-3.3-70b-versatile", "gpt-4o-mini").</summary>
    public string ModelId { get; set; } = string.Empty;
}
