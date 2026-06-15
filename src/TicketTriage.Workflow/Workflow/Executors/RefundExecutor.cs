using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.Logging;
using TicketTriage.Workflow.Domain.Models;

namespace TicketTriage.Workflow.Workflow.Executors;

/// <summary>
/// Terminal executor for the "Refund" route. Handles refund requests
/// deterministically with plain code - no LLM call - per AGENTS.md §7
/// ("use plain code for everything else"). In a real system this would
/// call a payments/refund service; here it produces the workflow outcome
/// that such a service call would be based on.
/// </summary>
public sealed class RefundExecutor : ReflectingExecutor<RefundExecutor>, IMessageHandler<RoutedTicket, WorkflowOutcome>
{
    private readonly ILogger<RefundExecutor> _logger;

    public RefundExecutor(ILogger<RefundExecutor> logger) : base("Refund")
    {
        _logger = logger;
    }

    public ValueTask<WorkflowOutcome> HandleAsync(RoutedTicket routed, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var ticket = routed.Classified.Ticket;

        var outcome = new WorkflowOutcome(
            TicketId: ticket.TicketId,
            Route: routed.Route,
            Summary: $"Refund initiated for ticket {ticket.TicketId} from {ticket.CustomerName}.");

        _logger.LogInformation("Refund flow completed for ticket {TicketId}", ticket.TicketId);

        return ValueTask.FromResult(outcome);
    }
}
