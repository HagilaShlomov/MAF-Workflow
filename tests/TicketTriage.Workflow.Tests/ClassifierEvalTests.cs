using System.Reflection;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using TicketTriage.Workflow.Agents;
using TicketTriage.Workflow.Domain.Models;
using TicketTriage.Workflow.Infrastructure;

namespace TicketTriage.Workflow.Tests;

/// <summary>
/// Eval tests that call the real classifier agent (no mocks) with labeled
/// ticket texts and check the structured output it returns. These hit a live
/// LLM endpoint, so they are slow, non-deterministic, and require
/// "ChatModel:ApiKey" to be configured (same user-secrets store as
/// TicketTriage.Workflow - see AGENTS.md §3). Excluded from fast unit test
/// runs via the "Integration" trait:
/// <c>dotnet test --filter "Category!=Integration"</c>.
/// </summary>
[Trait("Category", "Integration")]
public class ClassifierEvalTests
{
    private readonly AIAgent _agent;

    public ClassifierEvalTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();

        var chatModelOptions = new ChatModelOptions
        {
            Endpoint = configuration[$"{ChatModelOptions.SectionName}:{nameof(ChatModelOptions.Endpoint)}"] ?? string.Empty,
            ApiKey = configuration[$"{ChatModelOptions.SectionName}:{nameof(ChatModelOptions.ApiKey)}"] ?? string.Empty,
            ModelId = configuration[$"{ChatModelOptions.SectionName}:{nameof(ChatModelOptions.ModelId)}"] ?? string.Empty,
        };

        var chatClient = ChatClientFactory.Create(chatModelOptions);
        _agent = ClassifierAgentFactory.Create(chatClient);
    }

    public static TheoryData<string, string, string, TicketCategory, TicketUrgency,TicketUrgency, TicketConfidence?> LabeledTickets => new()
    {
        {
            "Maria Lopez",
            "Charged the wrong plan price",
            "My invoice this month shows $59 but I'm on the $49 plan. Can someone check my billing? No rush, just want it corrected before next month.",
            TicketCategory.Billing,
            TicketUrgency.Low,
            TicketUrgency.Medium,
            null
        },
        {
            "David Kim",
            "Can't log into my account",
            "I've tried resetting my password three times today and the reset email never arrives. I need access today to finish payroll for my team.",
            TicketCategory.AccountAccess,
            TicketUrgency.High,
            TicketUrgency.High,
            null
        },
        {
            "Sophie Turner",
            "Refund for returned order #55321",
            "I returned order #55321 two weeks ago and the tracking shows it was received by your warehouse, but I haven't gotten my $120 refund yet.",
            TicketCategory.Refund,
            TicketUrgency.Medium,
            TicketUrgency.High,
            null
        },
        {
            "Jordan Lee",
            "Something feels off",
            "I'm having a strange issue and not sure what category this even falls under. Something feels off but I can't pinpoint it. Can you help?",
            TicketCategory.General,
            TicketUrgency.Low,
            TicketUrgency.Medium,
            TicketConfidence.Low
        },
        {
            "Amir Hassan",
            "URGENT: account hacked",
            "Someone else logged into my account and changed my password and email. I no longer have access and think I've been hacked - please lock my account immediately.",
            TicketCategory.AccountAccess,
            TicketUrgency.Critical,
            TicketUrgency.Critical,
            null
        },
    };

    [Theory]
    [MemberData(nameof(LabeledTickets))]
    public async Task Classifier_ProducesExpectedCategoryAndUrgency(
        string customerName,
        string subject,
        string body,
        TicketCategory expectedCategory,
        //TicketUrgency expectedUrgency,
        TicketUrgency expectedMinUrgency,
        TicketUrgency expectedMaxUrgency,
        TicketConfidence? expectedConfidence)
    {
        var prompt =
            $"""
            Customer: {customerName}
            Subject: {subject}
            Body:
            {body}
            """;

        var response = await _agent.RunAsync<TicketClassification>(prompt);
        var classification = response.Result;

        Assert.Equal(expectedCategory, classification.Category);
        //Assert.Equal(expectedUrgency, classification.Urgency);
        
        Assert.True(
        classification.Urgency >= expectedMinUrgency &&
        classification.Urgency <= expectedMaxUrgency,
        $"Expected urgency between {expectedMinUrgency} and {expectedMaxUrgency}, got {classification.Urgency}");


        if (expectedConfidence is not null)
        {
            Assert.Equal(expectedConfidence, classification.Confidence);
        }
    }
}
