using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.Logging;
using TicketTriage.Workflow.Domain.Models;

namespace TicketTriage.Workflow.Workflow.Executors;

/// <summary>
/// Handles the "AutoReply" route. Wraps the reply-drafting <see cref="AIAgent"/>
/// (built by <see cref="TicketTriage.Workflow.Agents.DraftReplyAgentFactory"/>)
/// to produce a structured <see cref="DraftReply"/> for tickets the router
/// decided can be answered automatically without human review.
/// </summary>
public sealed class DraftReplyExecutor : ReflectingExecutor<DraftReplyExecutor>, IMessageHandler<RoutedTicket, ReplyToSend>
{
    private readonly AIAgent _agent;
    private readonly ILogger<DraftReplyExecutor> _logger;

    public DraftReplyExecutor(AIAgent agent, ILogger<DraftReplyExecutor> logger) : base("DraftReply")
    {
        _agent = agent;
        _logger = logger;
    }

    public async ValueTask<ReplyToSend> HandleAsync(RoutedTicket routed, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var ticket = routed.Classified.Ticket;
        var classification = routed.Classified.Classification;

        var prompt =
            $"""
            Customer: {ticket.CustomerName}
            Subject: {ticket.Subject}
            Category: {classification.Category}
            Body:
            {ticket.Body}
            """;

        var response = await _agent.RunAsync<DraftReply>(prompt, cancellationToken: cancellationToken);
        var draft = response.Result;

        _logger.LogInformation(
            "Drafted reply for ticket {TicketId} with subject \"{Subject}\"",
            ticket.TicketId, draft.Subject);

        return new ReplyToSend(ticket, draft);
    }
}
