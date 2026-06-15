using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.Logging;
using TicketTriage.Workflow.Domain.Models;

namespace TicketTriage.Workflow.Workflow.Executors;

/// <summary>
/// Terminal executor for the "AutoReply" route. Pure code - "sends" the
/// drafted reply (simulated here by logging it, since this sample has no
/// real email/ticketing integration) and yields the final workflow outcome.
/// </summary>
public sealed class SendReplyExecutor : ReflectingExecutor<SendReplyExecutor>, IMessageHandler<ReplyToSend, WorkflowOutcome>
{
    private readonly ILogger<SendReplyExecutor> _logger;

    public SendReplyExecutor(ILogger<SendReplyExecutor> logger) : base("SendReply")
    {
        _logger = logger;
    }

    public ValueTask<WorkflowOutcome> HandleAsync(ReplyToSend reply, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var outcome = new WorkflowOutcome(
            TicketId: reply.Ticket.TicketId,
            Route: TicketRoute.AutoReply,
            Summary: $"Reply sent to {reply.Ticket.CustomerName}. Subject: \"{reply.Draft.Subject}\". Body: {reply.Draft.Body}");

        _logger.LogInformation("Sent reply for ticket {TicketId}", reply.Ticket.TicketId);

        return ValueTask.FromResult(outcome);
    }
}
