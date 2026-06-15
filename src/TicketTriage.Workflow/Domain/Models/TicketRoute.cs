namespace TicketTriage.Workflow.Domain.Models;

/// <summary>
/// The path a ticket takes after <see cref="TicketTriage.Workflow.Workflow.Executors.RouterExecutor"/>
/// makes its decision. Each value corresponds to one outgoing edge of the router.
/// </summary>
public enum TicketRoute
{
    /// <summary>Send to a human reviewer via the human-in-the-loop request port.</summary>
    HumanReview,

    /// <summary>Handle deterministically via the refund flow (no LLM call).</summary>
    Refund,

    /// <summary>Draft an automated reply with the reply-drafting agent.</summary>
    AutoReply
}
