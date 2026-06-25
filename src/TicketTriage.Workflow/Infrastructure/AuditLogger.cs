using System.Text.Json;
using TicketTriage.Workflow.Domain.Models;

namespace TicketTriage.Workflow.Infrastructure;

public static class AuditLogger
{
    private static readonly string LogPath = "audit.jsonl";

    public static async Task LogAsync(
        IncomingTicket ticket,
        TicketClassification? classification,
        string route,
        string outcome)
    {
        var entry = new
        {
            timestamp = DateTime.UtcNow.ToString("o"),
            ticketId = Guid.NewGuid().ToString()[..8],
            customer = ticket.CustomerName,
            subject = ticket.Subject,
            category = classification?.Category.ToString() ?? "unknown",
            urgency = classification?.Urgency.ToString() ?? "unknown",
            confidence = classification != null ? classification.Confidence.ToString() : "n/a",
            missingInfo = classification?.MissingInfo ?? false,
            route,
            outcome
        };

        var line = JsonSerializer.Serialize(entry);
        await File.AppendAllTextAsync(LogPath, line + Environment.NewLine);
        Console.WriteLine($"  -> audit_log          : written to {LogPath}");
    }
}