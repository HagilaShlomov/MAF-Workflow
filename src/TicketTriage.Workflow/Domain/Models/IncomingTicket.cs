namespace TicketTriage.Workflow.Domain.Models;

/// <summary>
/// Raw support ticket as received from the customer, before any normalization.
/// This is the input type for the workflow's <see cref="TicketTriage.Workflow.Workflow.Executors.PreprocessExecutor"/>.
/// </summary>
public record IncomingTicket(string CustomerName, string Subject, string Body);
