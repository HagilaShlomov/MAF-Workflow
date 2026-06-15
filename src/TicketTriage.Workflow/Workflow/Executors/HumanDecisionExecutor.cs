using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.Logging;
using TicketTriage.Workflow.Domain.Models;

namespace TicketTriage.Workflow.Workflow.Executors;

/// <summary>
/// Terminal executor for the "HumanReview" route. Receives the
/// <see cref="HumanReviewDecision"/> sent back through the workflow's
/// human-in-the-loop request port (see
/// <see cref="TicketTriage.Workflow.Workflow.SupportTriageWorkflowFactory"/>'s
/// human review port) and yields the final workflow outcome based on the
/// reviewer's decision.
/// </summary>
public sealed class HumanDecisionExecutor : ReflectingExecutor<HumanDecisionExecutor>, IMessageHandler<HumanReviewDecision, WorkflowOutcome>
{
    private readonly ILogger<HumanDecisionExecutor> _logger;

    public HumanDecisionExecutor(ILogger<HumanDecisionExecutor> logger) : base("HumanDecision")
    {
        _logger = logger;
    }

    public ValueTask<WorkflowOutcome> HandleAsync(HumanReviewDecision decision, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var outcome = new WorkflowOutcome(
            TicketId: decision.TicketId,
            Route: TicketRoute.HumanReview,
            Summary: decision.Approved
                ? $"Human reviewer approved ticket {decision.TicketId}. Notes: {decision.Notes}"
                : $"Human reviewer rejected ticket {decision.TicketId}. Notes: {decision.Notes}");

        _logger.LogInformation(
            "Human review completed for ticket {TicketId} (approved: {Approved})",
            decision.TicketId, decision.Approved);

        return ValueTask.FromResult(outcome);
    }
}
