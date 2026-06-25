namespace TicketTriage.Workflow.Domain.Models;

/// <summary>
/// How confident the classifier is in its classification of a ticket.
/// </summary>
public enum TicketConfidence
{
    Low,
    Medium,
    High
}
