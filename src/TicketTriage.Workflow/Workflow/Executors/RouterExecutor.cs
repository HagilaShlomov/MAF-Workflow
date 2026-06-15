using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.Logging;
using TicketTriage.Workflow.Domain.Models;

namespace TicketTriage.Workflow.Workflow.Executors;

/// <summary>
/// Third required executor. Pure code - applies the business rules that
/// decide what happens to a classified ticket, encoding them explicitly
/// (per AGENTS.md §7: "Use conditional / switch-case edges to encode
/// business rules explicitly - don't hide routing logic inside agent
/// prompts") rather than asking the classifier to also decide routing.
///
/// The <see cref="Microsoft.Agents.AI.Workflows.WorkflowBuilder"/> graph
/// attaches its conditional edges to this executor's output, branching on
/// <see cref="RoutedTicket.Route"/>.
/// </summary>
public sealed class RouterExecutor : ReflectingExecutor<RouterExecutor>, IMessageHandler<ClassifiedTicket, RoutedTicket>
{
    private readonly ILogger<RouterExecutor> _logger;

    public RouterExecutor(ILogger<RouterExecutor> logger) : base("Router")
    {
        _logger = logger;
    }

    public ValueTask<RoutedTicket> HandleAsync(ClassifiedTicket ticket, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var route = DetermineRoute(ticket.Classification);

        _logger.LogInformation(
            "Routing ticket {TicketId} ({Category}/{Urgency}) to {Route}",
            ticket.Ticket.TicketId, ticket.Classification.Category, ticket.Classification.Urgency, route);

        return ValueTask.FromResult(new RoutedTicket(ticket, route));
    }

    /// <summary>
    /// Pure routing decision, exposed as a static method so it can be unit
    /// tested directly without constructing an executor or running the workflow.
    ///
    /// - Missing information or critical urgency always escalates to a human.
    /// - Refund requests (with sufficient info) go to the deterministic refund flow.
    /// - Everything else gets an automated drafted reply.
    /// </summary>
    public static TicketRoute DetermineRoute(TicketClassification classification)
    {
        if (classification.MissingInfo || classification.Urgency == TicketUrgency.Critical)
        {
            return TicketRoute.HumanReview;
        }

        if (classification.Category == TicketCategory.Refund)
        {
            return TicketRoute.Refund;
        }

        return TicketRoute.AutoReply;
    }
}
