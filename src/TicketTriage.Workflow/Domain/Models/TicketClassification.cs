using System.Text.Json.Serialization;

namespace TicketTriage.Workflow.Domain.Models;

/// <summary>
/// Structured output produced by the classifier agent
/// (<see cref="TicketTriage.Workflow.Workflow.Executors.ClassifierExecutor"/>).
/// This type is bound to the model's response format via
/// <c>ChatResponseFormat.ForJsonSchema</c>, so the model is required to return
/// JSON matching this exact shape - no free-text parsing.
/// </summary>
public sealed class TicketClassification
{
    [JsonPropertyName("category")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TicketCategory Category { get; set; }

    [JsonPropertyName("urgency")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TicketUrgency Urgency { get; set; }

    [JsonPropertyName("missing_info")]
    public bool MissingInfo { get; set; }

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = string.Empty;
}
