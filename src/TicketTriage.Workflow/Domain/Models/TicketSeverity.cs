namespace TicketTriage.Workflow.Domain.Models;

/// <summary>
/// How serious the underlying impact of a ticket is, independent of how
/// quickly it needs a response. <see cref="Critical"/> tickets are escalated
/// to a human reviewer when combined with certain categories.
/// </summary>
public enum TicketSeverity
{
    Low,
    Medium,
    High,
    Critical
}
