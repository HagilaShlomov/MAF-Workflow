using TicketTriage.Workflow.Domain.Models;
using TicketTriage.Workflow.Workflow.Executors;

namespace TicketTriage.Workflow.Tests;

public class RouterExecutorTests
{
    private static SupportTicket EmptyTicket(bool isRedFlag = false) => new(
        TicketId: "test-id",
        CustomerName: "Test User",
        Subject: "Test Subject",
        Body: "Test Body",
        ReceivedAtUtc: DateTimeOffset.UtcNow,
        IsRedFlag: isRedFlag);

    private static ClassifiedTicket Classify(
        TicketCategory category,
        TicketUrgency urgency,
        TicketSeverity severity,
        TicketConfidence confidence,
        bool missingInfo,
        bool isRedFlag = false) => new(
            EmptyTicket(isRedFlag),
            new TicketClassification
            {
                Category = category,
                Urgency = urgency,
                MissingInfo = missingInfo,
                Severity = severity,
                Confidence = confidence,
                Reasoning = "test reasoning",
            });

    [Fact]
    public void MissingInfo_AlwaysEscalatesToHumanReview()
    {
        var classified = Classify(TicketCategory.General, TicketUrgency.Low, TicketSeverity.Medium, TicketConfidence.Medium, missingInfo: true);
        Assert.Equal(TicketRoute.HumanReview, RouterExecutor.DetermineRoute(classified));
    }

    [Fact]
    public void CriticalUrgency_AlwaysEscalatesToHumanReview_EvenForRefund()
    {
        var classified = Classify(TicketCategory.Refund, TicketUrgency.Critical, TicketSeverity.Medium, TicketConfidence.Medium, missingInfo: false);
        Assert.Equal(TicketRoute.HumanReview, RouterExecutor.DetermineRoute(classified));
    }

    [Theory]
    [InlineData(TicketUrgency.Low)]
    [InlineData(TicketUrgency.Medium)]
    [InlineData(TicketUrgency.High)]
    public void RefundCategory_WithoutMissingInfoOrCriticalUrgency_RoutesToRefund(TicketUrgency urgency)
    {
        var classified = Classify(TicketCategory.Refund, urgency, TicketSeverity.Medium, TicketConfidence.Medium, missingInfo: false);
        Assert.Equal(TicketRoute.Refund, RouterExecutor.DetermineRoute(classified));
    }

    [Theory]
    [InlineData(TicketCategory.General)]
    [InlineData(TicketCategory.Technical)]
    [InlineData(TicketCategory.Billing)]
    [InlineData(TicketCategory.AccountAccess)]
    public void NonRefundCategory_WithoutMissingInfoOrCriticalUrgency_RoutesToAutoReply(TicketCategory category)
    {
        var classified = Classify(category, TicketUrgency.Medium, TicketSeverity.Medium, TicketConfidence.Medium, missingInfo: false);
        Assert.Equal(TicketRoute.AutoReply, RouterExecutor.DetermineRoute(classified));
    }

    [Fact]
    public void AccountAccess_WithCriticalSeverity_EscalatesToHumanReview()
    {
        var classified = Classify(TicketCategory.AccountAccess, TicketUrgency.Medium, TicketSeverity.Critical, TicketConfidence.Medium, missingInfo: false);
        Assert.Equal(TicketRoute.HumanReview, RouterExecutor.DetermineRoute(classified));
    }

    [Fact]
    public void CriticalSeverity_WithoutAccountAccessCategory_DoesNotEscalate()
    {
        var classified = Classify(TicketCategory.General, TicketUrgency.Medium, TicketSeverity.Critical, TicketConfidence.Medium, missingInfo: false);
        Assert.Equal(TicketRoute.AutoReply, RouterExecutor.DetermineRoute(classified));
    }

    [Fact]
    public void LowConfidence_AlwaysEscalatesToHumanReview()
    {
        var classified = Classify(TicketCategory.General, TicketUrgency.Medium, TicketSeverity.Medium, TicketConfidence.Low, missingInfo: false);
        Assert.Equal(TicketRoute.HumanReview, RouterExecutor.DetermineRoute(classified));
    }

    [Fact]
    public void RedFlag_AlwaysEscalatesToHumanReview()
    {
        var classified = Classify(TicketCategory.General, TicketUrgency.Low, TicketSeverity.Low, TicketConfidence.High, missingInfo: false, isRedFlag: true);
        Assert.Equal(TicketRoute.HumanReview, RouterExecutor.DetermineRoute(classified));
    }
}