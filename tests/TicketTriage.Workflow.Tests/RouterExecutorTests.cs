using TicketTriage.Workflow.Domain.Models;
using TicketTriage.Workflow.Workflow.Executors;

namespace TicketTriage.Workflow.Tests;

/// <summary>
/// Tests for <see cref="RouterExecutor.DetermineRoute"/>, the pure routing
/// decision that backs the workflow's conditional edges. Exercised directly
/// (without constructing an executor or running the workflow) to confirm the
/// at-least-two-conditional-routes and human-escalation requirements from
/// AGENTS.md §14 are encoded correctly.
/// </summary>
public class RouterExecutorTests
{
    private static TicketClassification Classify(
        TicketCategory category,
        TicketUrgency urgency,
        TicketSeverity severity,
        TicketConfidence confidence,
        bool missingInfo) => new()
        {
            Category = category,
            Urgency = urgency,
            MissingInfo = missingInfo,
            Severity = severity,
            Confidence = confidence,
            Reasoning = "test reasoning",
        };

    [Fact]
    public void MissingInfo_AlwaysEscalatesToHumanReview()
    {
        var classification = Classify(TicketCategory.General, TicketUrgency.Low, severity: TicketSeverity.Medium, confidence: TicketConfidence.Medium, missingInfo: true);

        var route = RouterExecutor.DetermineRoute(classification);

        Assert.Equal(TicketRoute.HumanReview, route);
    }

    [Fact]
    public void CriticalUrgency_AlwaysEscalatesToHumanReview_EvenForRefund()
    {
        var classification = Classify(TicketCategory.Refund, TicketUrgency.Critical, severity: TicketSeverity.Medium, confidence: TicketConfidence.Medium, missingInfo: false);

        var route = RouterExecutor.DetermineRoute(classification);

        Assert.Equal(TicketRoute.HumanReview, route);
    }

    [Theory]
    [InlineData(TicketUrgency.Low)]
    [InlineData(TicketUrgency.Medium)]
    [InlineData(TicketUrgency.High)]
    public void RefundCategory_WithoutMissingInfoOrCriticalUrgency_RoutesToRefund(TicketUrgency urgency)
    {
        var classification = Classify(TicketCategory.Refund, urgency, severity: TicketSeverity.Medium, confidence: TicketConfidence.Medium, missingInfo: false);

        var route = RouterExecutor.DetermineRoute(classification);

        Assert.Equal(TicketRoute.Refund, route);
    }

    [Theory]
    [InlineData(TicketCategory.General)]
    [InlineData(TicketCategory.Technical)]
    [InlineData(TicketCategory.Billing)]
    [InlineData(TicketCategory.AccountAccess)]
    public void NonRefundCategory_WithoutMissingInfoOrCriticalUrgency_RoutesToAutoReply(TicketCategory category)
    {
        var classification = Classify(category, TicketUrgency.Medium, severity: TicketSeverity.Medium, confidence: TicketConfidence.Medium, missingInfo: false);

        var route = RouterExecutor.DetermineRoute(classification);

        Assert.Equal(TicketRoute.AutoReply, route);
    }

    [Fact]
    public void AccountAccess_WithCriticalSeverity_EscalatesToHumanReview()
    {
        var classification = Classify(TicketCategory.AccountAccess, TicketUrgency.Medium, severity: TicketSeverity.Critical, confidence: TicketConfidence.Medium, missingInfo: false);

        var route = RouterExecutor.DetermineRoute(classification);

        Assert.Equal(TicketRoute.HumanReview, route);
    }

    [Fact]
    public void CriticalSeverity_WithoutAccountAccessCategory_DoesNotEscalate()
    {
        var classification = Classify(TicketCategory.General, TicketUrgency.Medium, severity: TicketSeverity.Critical, confidence: TicketConfidence.Medium, missingInfo: false);

        var route = RouterExecutor.DetermineRoute(classification);

        Assert.Equal(TicketRoute.AutoReply, route);
    }

    [Fact]
    public void LowConfidence_AlwaysEscalatesToHumanReview()
    {
        var classification = Classify(TicketCategory.General, TicketUrgency.Medium, severity: TicketSeverity.Medium, confidence: TicketConfidence.Low, missingInfo: false);

        var route = RouterExecutor.DetermineRoute(classification);

        Assert.Equal(TicketRoute.HumanReview, route);
    }
}
