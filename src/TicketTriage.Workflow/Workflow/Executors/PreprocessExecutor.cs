using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.Logging;
using TicketTriage.Workflow.Domain.Models;
using TicketTriage.Workflow.Domain.Validation;

namespace TicketTriage.Workflow.Workflow.Executors;

/// <summary>
/// First executor in the graph. Normalizes a raw <see cref="IncomingTicket"/>
/// into a <see cref="SupportTicket"/>: assigns a ticket ID, records the
/// received timestamp, trims whitespace, and runs deterministic red-flag
/// detection before the LLM classifier.
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
        var isRedFlag = RedFlagDetector.IsCritical(ticket.Subject, ticket.Body);

        var supportTicket = new SupportTicket(
            TicketId: Guid.NewGuid().ToString("N")[..8],
            CustomerName: ticket.CustomerName.Trim(),
            Subject: ticket.Subject.Trim(),
            Body: ticket.Body.Trim(),
            ReceivedAtUtc: DateTimeOffset.UtcNow,
            IsRedFlag: isRedFlag);

        if (isRedFlag)
        {
            _logger.LogWarning(
                "Red flag detected for ticket {TicketId} from {CustomerName}: \"{Subject}\"",
                supportTicket.TicketId, supportTicket.CustomerName, supportTicket.Subject);
        }
        else
        {
            _logger.LogInformation(
                "Preprocessed ticket {TicketId} from {CustomerName}: \"{Subject}\"",
                supportTicket.TicketId, supportTicket.CustomerName, supportTicket.Subject);
        }

        return ValueTask.FromResult(supportTicket);
    }
}