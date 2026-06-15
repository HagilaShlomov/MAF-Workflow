namespace TicketTriage.Workflow.Domain.Models;

/// <summary>
/// Final result produced by any terminal executor in the workflow
/// (<see cref="TicketTriage.Workflow.Workflow.Executors.RefundExecutor"/>,
/// <see cref="TicketTriage.Workflow.Workflow.Executors.SendReplyExecutor"/>,
/// <see cref="TicketTriage.Workflow.Workflow.Executors.HumanDecisionExecutor"/>).
/// Yielded via <c>IWorkflowContext.YieldOutputAsync</c> and surfaced to the
/// caller as a <c>WorkflowOutputEvent</c>, giving the sample run a single
/// type to inspect regardless of which route a ticket took.
/// </summary>
public record WorkflowOutcome(string TicketId, TicketRoute Route, string Summary);
