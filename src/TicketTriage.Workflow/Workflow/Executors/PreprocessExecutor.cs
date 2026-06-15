using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.Logging;
using TicketTriage.Workflow.Domain.Models;

namespace TicketTriage.Workflow.Workflow.Executors;

/// <summary>
/// First executor in the graph. Normalizes a raw <see cref="IncomingTicket"/>
/// into a <see cref="SupportTicket"/>: assigns a ticket ID, records the
/// received timestamp, and trims whitespace from customer-supplied text.
///
/// Pure code, no LLM call - per AGENTS.md's "use plain code for everything
/// that isn't a judgment call" rule. This satisfies the assignment's
/// "Preprocess Executor (plain function executor)" requirement.
///
/// Implements <see cref="IMessageHandler{TIn,TOut}"/> rather than the
/// single-type-parameter <c>IMessageHandler&lt;TIn&gt;</c>: only the two-type
/// overload registers its result type with <see cref="ProtocolBuilder"/>, which
/// is required before <see cref="ExecutorOptions.AutoSendMessageHandlerResultObject"/>
/// (on by default) is allowed to forward the returned <see cref="SupportTicket"/>
/// along this executor's outgoing edge.
/// </summary>
public sealed class PreprocessExecutor : ReflectingExecutor<PreprocessExecutor>, IMessageHandler<IncomingTicket, SupportTicket>
{
    private readonly ILogger<PreprocessExecutor> _logger;

    public PreprocessExecutor(ILogger<PreprocessExecutor> logger) : base("Preprocess")
    {
        _logger = logger;
    }

    public ValueTask<SupportTicket> HandleAsync(IncomingTicket ticket, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var supportTicket = new SupportTicket(
            TicketId: Guid.NewGuid().ToString("N")[..8],
            CustomerName: ticket.CustomerName.Trim(),
            Subject: ticket.Subject.Trim(),
            Body: ticket.Body.Trim(),
            ReceivedAtUtc: DateTimeOffset.UtcNow);

        _logger.LogInformation(
            "Preprocessed ticket {TicketId} from {CustomerName}: \"{Subject}\"",
            supportTicket.TicketId, supportTicket.CustomerName, supportTicket.Subject);

        return ValueTask.FromResult(supportTicket);
    }
}
