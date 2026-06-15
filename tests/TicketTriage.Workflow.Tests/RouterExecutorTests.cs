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
        bool missingInfo) => new()
        {
            Category = category,
            Urgency = urgency,
            MissingInfo = missingInfo,
            Reasoning = "test reasoning",
        };

    [Fact]
    public void MissingInfo_AlwaysEscalatesToHumanReview()
    {
        var classification = Classify(TicketCategory.General, TicketUrgency.Low, missingInfo: true);

        var route = RouterExecutor.DetermineRoute(classification);

        Assert.Equal(TicketRoute.HumanReview, route);
    }

    [Fact]
    public void CriticalUrgency_AlwaysEscalatesToHumanReview_EvenForRefund()
    {
        var classification = Classify(TicketCategory.Refund, TicketUrgency.Critical, missingInfo: false);

        var route = RouterExecutor.DetermineRoute(classification);

        Assert.Equal(TicketRoute.HumanReview, route);
    }

    [Theory]
    [InlineData(TicketUrgency.Low)]
    [InlineData(TicketUrgency.Medium)]
    [InlineData(TicketUrgency.High)]
    public void RefundCategory_WithoutMissingInfoOrCriticalUrgency_RoutesToRefund(TicketUrgency urgency)
    {
        var classification = Classify(TicketCategory.Refund, urgency, missingInfo: false);

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
        var classification = Classify(category, TicketUrgency.Medium, missingInfo: false);

        var route = RouterExecutor.DetermineRoute(classification);

        Assert.Equal(TicketRoute.AutoReply, route);
    }
}
