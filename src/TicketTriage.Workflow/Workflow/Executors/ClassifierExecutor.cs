using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.Logging;
using TicketTriage.Workflow.Domain.Models;
using TicketTriage.Workflow.Domain.Validation;

namespace TicketTriage.Workflow.Workflow.Executors;

/// <summary>
/// Second executor in the graph. Wraps the classifier <see cref="AIAgent"/>
/// (built by <see cref="TicketTriage.Workflow.Agents.ClassifierAgentFactory"/>)
/// to produce a structured <see cref="TicketClassification"/> for the ticket,
/// validates it, and forwards a <see cref="ClassifiedTicket"/> downstream.
///
/// This is the assignment's required "Classifier Executor (agent-based
/// executor)" and is the source of the workflow's structured output
/// (Category / Urgency / MissingInfo).
/// </summary>
public sealed class ClassifierExecutor : ReflectingExecutor<ClassifierExecutor>, IMessageHandler<SupportTicket, ClassifiedTicket>
{
    private readonly AIAgent _agent;
    private readonly ILogger<ClassifierExecutor> _logger;

    public ClassifierExecutor(AIAgent agent, ILogger<ClassifierExecutor> logger) : base("Classifier")
    {
        _agent = agent;
        _logger = logger;
    }

    public async ValueTask<ClassifiedTicket> HandleAsync(SupportTicket ticket, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var prompt =
            $"""
            Customer: {ticket.CustomerName}
            Subject: {ticket.Subject}
            Body:
            {ticket.Body}
            """;

        var response = await _agent.RunAsync<TicketClassification>(prompt, cancellationToken: cancellationToken);
        var classification = response.Result;

        var errors = TicketClassificationValidator.Validate(classification);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Classifier returned an invalid result for ticket {ticket.TicketId}: {string.Join("; ", errors)}");
        }

        _logger.LogInformation(
            "Classified ticket {TicketId} as {Category}/{Urgency} (missing info: {MissingInfo}). Reason: {Reasoning}",
            ticket.TicketId, classification.Category, classification.Urgency, classification.MissingInfo, classification.Reasoning);

        return new ClassifiedTicket(ticket, classification);
    }
}
