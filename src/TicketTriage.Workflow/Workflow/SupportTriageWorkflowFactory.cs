using Microsoft.Agents.AI.Workflows;
using TicketTriage.Workflow.Domain.Models;
using TicketTriage.Workflow.Workflow.Executors;

namespace TicketTriage.Workflow.Workflow;

/// <summary>
/// Assembles the support-ticket triage workflow graph using
/// <see cref="WorkflowBuilder"/>. This is the single place where the shape of
/// the workflow - which executors exist and how they are wired together - is
/// defined, matching the example graph in AGENTS.md §7:
///
/// <code>
/// Preprocess (code)
///    -&gt; Classify (agent, structured output)
///       -&gt; Router (code, conditional edges)
///          -&gt; Escalate to Human (request_info)
///          -&gt; Refund Flow (code)
///          -&gt; Draft Reply (agent) -&gt; Send (code)
/// </code>
/// </summary>
public static class SupportTriageWorkflowFactory
{
    /// <summary>
    /// Identifier of the human-in-the-loop request port. The host application's
    /// streaming loop (see <c>Program.cs</c>) responds to
    /// <see cref="RequestInfoEvent"/>s raised by this port.
    /// </summary>
    public const string HumanReviewPortId = "HumanReviewRequest";

    public static Microsoft.Agents.AI.Workflows.Workflow Create(
        PreprocessExecutor preprocess,
        ClassifierExecutor classifier,
        RouterExecutor router,
        RefundExecutor refund,
        DraftReplyExecutor draftReply,
        SendReplyExecutor sendReply,
        HumanDecisionExecutor humanDecision)
    {
        // The human-in-the-loop request port: the router hands a RoutedTicket
        // to it, the workflow run raises a RequestInfoEvent for the host to
        // answer, and the reviewer's HumanReviewDecision is forwarded on to
        // HumanDecisionExecutor.
        var humanReviewPort = RequestPort.Create<RoutedTicket, HumanReviewDecision>(HumanReviewPortId);

        return new WorkflowBuilder(preprocess)
            // Linear pipeline: Preprocess -> Classify -> Router.
            .AddEdge(preprocess, classifier)
            .AddEdge(classifier, router)

            // Conditional routes out of the Router - at least 2 are required
            // this graph has 3, one of which (HumanReview)
            // escalates via the request_info port.
            .AddEdge<RoutedTicket>(router, humanReviewPort, r => r?.Route == TicketRoute.HumanReview)
            .AddEdge<RoutedTicket>(router, refund, r => r?.Route == TicketRoute.Refund)
            .AddEdge<RoutedTicket>(router, draftReply, r => r?.Route == TicketRoute.AutoReply)

            // Continuations after the conditional routes.
            .AddEdge(humanReviewPort, humanDecision)
            .AddEdge(draftReply, sendReply)

            // Terminal executors - each yields a WorkflowOutcome via
            // IWorkflowContext.YieldOutputAsync.
            .WithOutputFrom(refund, sendReply, humanDecision)
            .Build();
    }
}
