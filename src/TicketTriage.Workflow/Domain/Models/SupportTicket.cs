namespace TicketTriage.Workflow.Domain.Models;

/// <summary>
/// A normalized support ticket with an assigned identifier and timestamp.
/// Produced by <see cref="TicketTriage.Workflow.Workflow.Executors.PreprocessExecutor"/>
/// and consumed by the classifier agent.
/// </summary>
public record SupportTicket(
    string TicketId,
    string CustomerName,
    string Subject,
    string Body,
    DateTimeOffset ReceivedAtUtc);
