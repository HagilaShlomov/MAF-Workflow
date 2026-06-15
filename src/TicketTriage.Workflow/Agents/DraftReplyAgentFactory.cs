using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using TicketTriage.Workflow.Domain.Models;

namespace TicketTriage.Workflow.Agents;

/// <summary>
/// Builds the single-purpose agent used by
/// <see cref="TicketTriage.Workflow.Workflow.Executors.DraftReplyExecutor"/> to
/// draft a reply for tickets the router has decided can be answered
/// automatically. This agent only drafts text - it never sends anything.
/// </summary>
public static class DraftReplyAgentFactory
{
    public const string AgentName = "DraftReplyAgent";

    private const string Instructions =
        """
        You are a support-reply drafting assistant. Write a short, professional
        email reply to the customer's support ticket.

        - subject: a concise reply subject line (you may prefix with "Re: ").
        - body: a friendly, professional reply that addresses the customer's
          request. Do not invent account-specific details you were not given.

        Return only the structured reply.
        """;

    /// <summary>Creates a reply-drafting agent bound to <see cref="DraftReply"/> structured output.</summary>
    public static AIAgent Create(IChatClient chatClient) =>
        new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            Name = AgentName,
            ChatOptions = new ChatOptions
            {
                Instructions = Instructions,
                ResponseFormat = ChatResponseFormat.ForJsonSchema(
                    AIJsonUtilities.CreateJsonSchema(typeof(DraftReply)))
            }
        });
}
