namespace TicketTriage.Workflow.Domain.Models;

/// <summary>
/// How quickly a ticket needs attention. <see cref="Critical"/> tickets are
/// always escalated to a human reviewer regardless of category.
/// </summary>
public enum TicketUrgency
{
    Low,
    Medium,
    High,
    Critical
}
