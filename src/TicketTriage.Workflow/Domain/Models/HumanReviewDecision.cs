namespace TicketTriage.Workflow.Domain.Models;

/// <summary>
/// The response a human reviewer sends back through the workflow's
/// human-in-the-loop request port after reviewing an escalated
/// <see cref="RoutedTicket"/>. Carries <see cref="TicketId"/> so the
/// terminal executor that receives this response can report which ticket
/// it belongs to (the request port itself only forwards this response
/// type, not the original request).
/// </summary>
public record HumanReviewDecision(string TicketId, bool Approved, string Notes);
