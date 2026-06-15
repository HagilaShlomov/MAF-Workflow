namespace TicketTriage.Workflow.Domain.Models;

/// <summary>
/// A drafted reply together with the ticket it answers. Output of
/// <see cref="TicketTriage.Workflow.Workflow.Executors.DraftReplyExecutor"/> and
/// input to <see cref="TicketTriage.Workflow.Workflow.Executors.SendReplyExecutor"/>.
/// </summary>
public record ReplyToSend(SupportTicket Ticket, DraftReply Draft);
