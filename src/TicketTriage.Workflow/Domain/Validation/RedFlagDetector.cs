namespace TicketTriage.Workflow.Domain.Validation;

/// <summary>
/// Deterministic red-flag detection based on keywords and patterns.
/// Runs before the LLM classifier to catch obvious critical cases fast,
/// without waiting for an LLM response.
/// </summary>
public static class RedFlagDetector
{
    private static readonly string[] CriticalKeywords =
    [
        "hacked", "hack", "breach", "stolen", "hijacked",
        "urgent", "critical", "emergency", "outage", "down",
        "cannot access", "locked out", "immediate", "asap"
    ];

    /// <summary>
    /// Returns true if the ticket subject or body contains any critical keyword.
    /// </summary>
    public static bool IsCritical(string subject, string body)
    {
        var text = $"{subject} {body}".ToLowerInvariant();
        return CriticalKeywords.Any(keyword => text.Contains(keyword));
    }
}