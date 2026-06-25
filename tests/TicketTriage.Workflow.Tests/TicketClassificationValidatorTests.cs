using TicketTriage.Workflow.Domain.Models;
using TicketTriage.Workflow.Domain.Validation;

namespace TicketTriage.Workflow.Tests;

/// <summary>
/// Tests for <see cref="TicketClassificationValidator.Validate"/>, the pure
/// guard that runs on the classifier agent's structured output before
/// <see cref="TicketTriage.Workflow.Workflow.Executors.RouterExecutor"/> uses
/// it for routing. Covers null, well-formed, and malformed model output.
/// </summary>
public class TicketClassificationValidatorTests
{
    [Fact]
    public void NullClassification_ReturnsSingleError()
    {
        var errors = TicketClassificationValidator.Validate(null);

        Assert.Equal(["Classifier returned no result."], errors);
    }

    [Fact]
    public void WellFormedClassification_ReturnsNoErrors()
    {
        var classification = new TicketClassification
        {
            Category = TicketCategory.Technical,
            Urgency = TicketUrgency.Medium,
            MissingInfo = false,
            Severity = TicketSeverity.Medium,
            Confidence = TicketConfidence.Medium,
            Reasoning = "Customer reports a login error.",
        };

        var errors = TicketClassificationValidator.Validate(classification);

        Assert.Empty(errors);
    }

    [Fact]
    public void EmptyReasoning_IsReportedAsError()
    {
        var classification = new TicketClassification
        {
            Category = TicketCategory.General,
            Urgency = TicketUrgency.Low,
            MissingInfo = false,
            Severity = TicketSeverity.Medium,
            Confidence = TicketConfidence.Medium,
            Reasoning = "   ",
        };

        var errors = TicketClassificationValidator.Validate(classification);

        Assert.Contains("Reasoning must not be empty.", errors);
    }

    [Fact]
    public void OutOfRangeCategoryAndUrgency_AreBothReportedAsErrors()
    {
        var classification = new TicketClassification
        {
            Category = (TicketCategory)999,
            Urgency = (TicketUrgency)999,
            MissingInfo = false,
            Severity = TicketSeverity.Medium,
            Confidence = TicketConfidence.Medium,
            Reasoning = "Customer reports a login error.",
        };

        var errors = TicketClassificationValidator.Validate(classification);

        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Contains("Category"));
        Assert.Contains(errors, e => e.Contains("Urgency"));
    }

    [Fact]
    public void OutOfRangeSeverityAndConfidence_AreBothReportedAsErrors()
    {
        var classification = new TicketClassification
        {
            Category = TicketCategory.General,
            Urgency = TicketUrgency.Medium,
            MissingInfo = false,
            Severity = (TicketSeverity)999,
            Confidence = (TicketConfidence)999,
            Reasoning = "Customer reports a login error.",
        };

        var errors = TicketClassificationValidator.Validate(classification);

        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.Contains("Severity"));
        Assert.Contains(errors, e => e.Contains("Confidence"));
    }
}
