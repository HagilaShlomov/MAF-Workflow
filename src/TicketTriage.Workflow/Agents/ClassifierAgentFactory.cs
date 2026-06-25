using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using TicketTriage.Workflow.Domain.Models;

namespace TicketTriage.Workflow.Agents;

/// <summary>
/// Builds the single-purpose agent used by
/// <see cref="TicketTriage.Workflow.Workflow.Executors.ClassifierExecutor"/> to
/// triage support tickets. The agent's only job is classification - it does
/// not draft replies, take actions, or hold conversation state, per the
/// "one clear responsibility per agent" rule.
/// </summary>
public static class ClassifierAgentFactory
{
    public const string AgentName = "TicketClassifierAgent";

    private const string Instructions =
        """
        You are a support ticket triage assistant. Read the customer's support
        ticket and classify it.

        - category: the best-fitting category for the request (General,
          Technical, Billing, AccountAccess, or Refund).
        - urgency: how quickly the ticket needs attention (Low, Medium, High,
          or Critical). Use "Critical" only for outages, security incidents,
          data loss, or safety issues.
        - severity: how serious the underlying impact is, independent of how
          fast it needs a response (Low, Medium, High, or Critical).
          - Low: cosmetic or minor inconvenience, no real impact on the
            customer's ability to use the product.
          - Medium: a real problem that degrades the customer's experience
            but has a workaround.
          - High: a significant problem with no workaround, blocking a core
            task for the customer.
          - Critical: severe business or customer impact - data loss,
            security breach, account compromise, or total loss of service.
        - confidence: how confident you are in this classification, given the
          information in the ticket (Low, Medium, High, or Critical).
          - Low: the ticket is vague or ambiguous; you are largely guessing.
          - Medium: you have a reasonable basis for the classification but
            some details are unclear or assumed.
          - High: the ticket clearly supports the classification with only
            minor ambiguity.
          - Critical: the ticket is unambiguous and you are certain of the
            classification.
        - missing_info: true only if the ticket lacks information that is
          REQUIRED before any action can be taken (e.g. no order number for a
          refund request, no account identifier for an account-access issue).
        - reasoning: one or two sentences explaining your decision.

        Return only the structured classification result.
        """;

    /// <summary>Creates a classifier agent bound to <see cref="TicketClassification"/> structured output.</summary>
    public static AIAgent Create(IChatClient chatClient) =>
        new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            Name = AgentName,
            ChatOptions = new ChatOptions
            {
                Instructions = Instructions,
                ResponseFormat = ChatResponseFormat.ForJsonSchema(
                    AIJsonUtilities.CreateJsonSchema(typeof(TicketClassification)))
            }
        });
}
