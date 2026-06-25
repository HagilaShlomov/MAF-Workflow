using System.Reflection;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TicketTriage.Workflow.Agents;
using TicketTriage.Workflow.Domain.Models;
using TicketTriage.Workflow.Infrastructure;
using TicketTriage.Workflow.Workflow;
using TicketTriage.Workflow.Workflow.Executors;

// ---------------------------------------------------------------------------
// Configuration: appsettings.json -> environment variables -> user secrets.
// Never hardcode the chat model endpoint/key/model id (AGENTS.md §3).
// ---------------------------------------------------------------------------
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

// ---------------------------------------------------------------------------
// Dependency injection: chat client, agents, and executors (AGENTS.md §3).
// The classifier and draft-reply agents are both AIAgent instances, so they
// are registered as keyed singletons to keep them distinct.
// ---------------------------------------------------------------------------
var services = new ServiceCollection();

services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

services.AddSingleton<IChatClient>(_ => ChatClientFactory.Create(chatModelOptions));

services.AddKeyedSingleton<AIAgent>(
    "Classifier",
    (sp, _) => ClassifierAgentFactory.Create(sp.GetRequiredService<IChatClient>()));

services.AddKeyedSingleton<AIAgent>(
    "DraftReply",
    (sp, _) => DraftReplyAgentFactory.Create(sp.GetRequiredService<IChatClient>()));

services.AddTransient<PreprocessExecutor>();
services.AddTransient<RouterExecutor>();
services.AddTransient<RefundExecutor>();
services.AddTransient<SendReplyExecutor>();
services.AddTransient<HumanDecisionExecutor>();

services.AddTransient(sp => new ClassifierExecutor(
    sp.GetRequiredKeyedService<AIAgent>("Classifier"),
    sp.GetRequiredService<ILogger<ClassifierExecutor>>()));

services.AddTransient(sp => new DraftReplyExecutor(
    sp.GetRequiredKeyedService<AIAgent>("DraftReply"),
    sp.GetRequiredService<ILogger<DraftReplyExecutor>>()));

await using var serviceProvider = services.BuildServiceProvider();

// ---------------------------------------------------------------------------
// Build the workflow graph (Step 5).
// ---------------------------------------------------------------------------
var workflow = SupportTriageWorkflowFactory.Create(
    serviceProvider.GetRequiredService<PreprocessExecutor>(),
    serviceProvider.GetRequiredService<ClassifierExecutor>(),
    serviceProvider.GetRequiredService<RouterExecutor>(),
    serviceProvider.GetRequiredService<RefundExecutor>(),
    serviceProvider.GetRequiredService<DraftReplyExecutor>(),
    serviceProvider.GetRequiredService<SendReplyExecutor>(),
    serviceProvider.GetRequiredService<HumanDecisionExecutor>());

// ---------------------------------------------------------------------------
// Sample run (Step 7): three tickets that each take a different route through
// the router - AutoReply, Refund, and HumanReview - so all conditional edges
// and the human-in-the-loop escalation are exercised end-to-end.
// ---------------------------------------------------------------------------
var sampleTickets = new[]
{
    new IncomingTicket(
        CustomerName: "Alice Nguyen",
        Subject: "Can't log into my account",
        Body: "I've been trying to log in for an hour and keep getting an " +
              "'invalid password' error even after resetting it. My account " +
              "email is alice@example.com."),

    new IncomingTicket(
        CustomerName: "Ben Carter",
        Subject: "Refund for order #48213",
        Body: "I was charged twice for order #48213. Please refund the " +
              "duplicate charge of $42.00 to my card."),

    new IncomingTicket(
        CustomerName: "Priya Shah",
        Subject: "URGENT: production database is down",
        Body: "Our production database has been down for 20 minutes and " +
              "customers cannot check out. This is a critical outage - " +
              "please escalate immediately."),
};

foreach (var ticket in sampleTickets)
{
    Console.WriteLine();
    Console.WriteLine("==============================================================");
    Console.WriteLine($"Submitting ticket from {ticket.CustomerName}: \"{ticket.Subject}\"");
    Console.WriteLine("==============================================================");

    await using var run = await InProcessExecution.RunStreamingAsync(workflow, ticket);

    await foreach (var evt in run.WatchStreamAsync())
    {
        switch (evt)
        {
            case ExecutorInvokedEvent invoked:
                Console.WriteLine($"  -> executor_invoked   : {invoked.ExecutorId}");
                break;

            case ExecutorCompletedEvent completed:
                Console.WriteLine($"  -> executor_completed : {completed.ExecutorId}");
                break;

            case RequestInfoEvent requestInfo:
                if (requestInfo.Request.TryGetDataAs<RoutedTicket>(out var routedTicket))
                {
                    Console.WriteLine($"  -> request_info       : human review requested for ticket {routedTicket.Classified.Ticket.TicketId}");
                    Console.WriteLine($"       category={routedTicket.Classified.Classification.Category}, " +
                                       $"urgency={routedTicket.Classified.Classification.Urgency}, " +
                                       $"missingInfo={routedTicket.Classified.Classification.MissingInfo}");

                    // Simulated human reviewer. A real deployment would surface
                    // this request (e.g. via a UI or ticketing system) and call
                    // SendResponseAsync only once the reviewer responds.
                    var decision = new HumanReviewDecision(
                        TicketId: routedTicket.Classified.Ticket.TicketId,
                        Approved: true,
                        Notes: "Approved by on-call reviewer (simulated).");

                    Console.WriteLine($"       reviewer decision     : approved={decision.Approved}, notes=\"{decision.Notes}\"");
                    await run.SendResponseAsync(requestInfo.Request.CreateResponse(decision));
                }

                break;

            case WorkflowOutputEvent output:
                var workflowOutcome = output.As<WorkflowOutcome>();
                Console.WriteLine($"  -> output             : {workflowOutcome}");
                await AuditLogger.LogAsync(
                    ticket,
                    null,
                    workflowOutcome?.Route.ToString() ?? "unknown",
                    workflowOutcome?.Summary ?? "unknown");
                break;

            case WorkflowErrorEvent error:
                Console.WriteLine($"  -> error              : {error.Exception}");
                break;
        }
    }
}
