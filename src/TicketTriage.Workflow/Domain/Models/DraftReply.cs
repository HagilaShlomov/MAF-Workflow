using System.Text.Json.Serialization;

namespace TicketTriage.Workflow.Domain.Models;

/// <summary>
/// Structured output produced by the reply-drafting agent
/// (<see cref="TicketTriage.Workflow.Workflow.Executors.DraftReplyExecutor"/>).
/// Bound to the model's response format via <c>ChatResponseFormat.ForJsonSchema</c>.
/// </summary>
public sealed class DraftReply
{
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}
