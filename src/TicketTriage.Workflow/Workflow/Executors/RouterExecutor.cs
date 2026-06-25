using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.Logging;
using TicketTriage.Workflow.Domain.Models;

namespace TicketTriage.Workflow.Workflow.Executors;

public sealed class RouterExecutor : ReflectingExecutor<RouterExecutor>, IMessageHandler<ClassifiedTicket, RoutedTicket>
{
    private readonly ILogger<RouterExecutor> _logger;

    public RouterExecutor(ILogger<RouterExecutor> logger) : base("Router")
    {
        _logger = logger;
    }

    public ValueTask<RoutedTicket> HandleAsync(ClassifiedTicket ticket, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var route = DetermineRoute(ticket);

        _logger.LogInformation(
            "Routing ticket {TicketId} ({Category}/{Urgency}) to {Route}",
            ticket.Ticket.TicketId, ticket.Classification.Category, ticket.Classification.Urgency, route);

        return ValueTask.FromResult(new RoutedTicket(ticket, route));
    }

    public static TicketRoute DetermineRoute(ClassifiedTicket classifiedTicket)
    {
        // Deterministic red-flag check — runs before any LLM-based rules
        if (classifiedTicket.Ticket.IsRedFlag)
        {
            return TicketRoute.HumanReview;
        }

        var classification = classifiedTicket.Classification;

        if (classification.MissingInfo || classification.Urgency == TicketUrgency.Critical)
        {
            return TicketRoute.HumanReview;
        }

        if (classification.Category == TicketCategory.Refund)
        {
            return TicketRoute.Refund;
        }

        if (classification.Category == TicketCategory.AccountAccess && classification.Severity == TicketSeverity.Critical)
        {
            return TicketRoute.HumanReview;
        }

        if (classification.Confidence == TicketConfidence.Low)
        {
            return TicketRoute.HumanReview;
        }

        return TicketRoute.AutoReply;
    }
}