namespace TicketTriage.Workflow.Domain.Models;

/// <summary>
/// A support ticket paired with its classification. Output of
/// <see cref="TicketTriage.Workflow.Workflow.Executors.ClassifierExecutor"/> and
/// input to <see cref="TicketTriage.Workflow.Workflow.Executors.RouterExecutor"/>.
/// </summary>
public record ClassifiedTicket(SupportTicket Ticket, TicketClassification Classification);
