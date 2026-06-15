using TicketTriage.Workflow.Domain.Models;

namespace TicketTriage.Workflow.Domain.Validation;

/// <summary>
/// Validates a <see cref="TicketClassification"/> returned by the classifier agent
/// before it is used for routing decisions. Pure business logic - no agent/LLM
/// dependencies - so it can be unit tested in isolation, including against
/// malformed or incomplete model output.
/// </summary>
public static class TicketClassificationValidator
{
    /// <summary>
    /// Returns a list of validation error messages. An empty list means the
    /// classification is valid and safe to route on.
    /// </summary>
    public static IReadOnlyList<string> Validate(TicketClassification? classification)
    {
        var errors = new List<string>();

        if (classification is null)
        {
            errors.Add("Classifier returned no result.");
            return errors;
        }

        if (!Enum.IsDefined(typeof(TicketCategory), classification.Category))
        {
            errors.Add($"Category value '{classification.Category}' is not a recognized ticket category.");
        }

        if (!Enum.IsDefined(typeof(TicketUrgency), classification.Urgency))
        {
            errors.Add($"Urgency value '{classification.Urgency}' is not a recognized urgency level.");
        }

        if (string.IsNullOrWhiteSpace(classification.Reasoning))
        {
            errors.Add("Reasoning must not be empty.");
        }

        return errors;
    }
}
