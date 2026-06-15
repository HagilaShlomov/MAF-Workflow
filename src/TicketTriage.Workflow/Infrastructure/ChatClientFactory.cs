using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

namespace TicketTriage.Workflow.Infrastructure;

/// <summary>
/// Builds the <see cref="IChatClient"/> used by every agent in the workflow.
/// Centralizing this keeps model wiring in one place and out of agent/workflow code.
/// Targets any OpenAI Chat Completions-compatible endpoint (Azure OpenAI's
/// "/openai/v1" surface, Groq, GitHub Models, etc.) via API-key auth.
/// </summary>
public static class ChatClientFactory
{
    public static IChatClient Create(ChatModelOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new InvalidOperationException(
                $"Configuration '{ChatModelOptions.SectionName}:{nameof(ChatModelOptions.Endpoint)}' is missing. " +
                "Set it in appsettings.json, an environment variable, or via 'dotnet user-secrets'.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException(
                $"Configuration '{ChatModelOptions.SectionName}:{nameof(ChatModelOptions.ApiKey)}' is missing. " +
                "Set it via 'dotnet user-secrets set \"ChatModel:ApiKey\" \"<key>\"' or an environment variable.");
        }

        if (string.IsNullOrWhiteSpace(options.ModelId))
        {
            throw new InvalidOperationException(
                $"Configuration '{ChatModelOptions.SectionName}:{nameof(ChatModelOptions.ModelId)}' is missing. " +
                "Set it in appsettings.json, an environment variable, or via 'dotnet user-secrets'.");
        }

        var client = new OpenAIClient(
            new ApiKeyCredential(options.ApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(options.Endpoint) });

        return client.GetChatClient(options.ModelId).AsIChatClient();
    }
}
