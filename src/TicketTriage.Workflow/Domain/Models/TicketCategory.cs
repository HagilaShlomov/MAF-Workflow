namespace TicketTriage.Workflow.Domain.Models;

/// <summary>
/// The kind of support request a ticket represents. Drives routing decisions
/// in <see cref="TicketTriage.Workflow.Workflow.Executors.RouterExecutor"/>.
/// </summary>
public enum TicketCategory
{
    General,
    Technical,
    Billing,
    AccountAccess,
    Refund
}
