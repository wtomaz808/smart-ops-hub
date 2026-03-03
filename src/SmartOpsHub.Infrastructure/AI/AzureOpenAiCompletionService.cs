using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using ChatMessage = SmartOpsHub.Core.Models.ChatMessage;

namespace SmartOpsHub.Infrastructure.AI;

public sealed class AzureOpenAiCompletionService : IAiCompletionService
{
    private readonly AzureOpenAIClient _openAiClient;
    private readonly string _defaultDeployment;
    private readonly ConcurrentDictionary<string, ChatClient> _chatClients = new();

    public AzureOpenAiCompletionService(IConfiguration configuration)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint configuration is required.");
        _defaultDeployment = configuration["AzureOpenAI:DeploymentName"]
            ?? ModelOption.Default.DeploymentName;

        var credential = new DefaultAzureCredential();
        _openAiClient = new AzureOpenAIClient(new Uri(endpoint), credential);
    }

    public async Task<string> GetCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<McpToolDefinition>? availableTools = null,
        string? deploymentName = null,
        CancellationToken cancellationToken = default)
    {
        var client = GetChatClient(deploymentName);
        var chatMessages = MapMessages(messages);
        var options = BuildOptions(availableTools);

        ChatCompletion completion = await client.CompleteChatAsync(chatMessages, options, cancellationToken);

        return completion.Content[0].Text ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<McpToolDefinition>? availableTools = null,
        string? deploymentName = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = GetChatClient(deploymentName);
        var chatMessages = MapMessages(messages);
        var options = BuildOptions(availableTools);

        await foreach (StreamingChatCompletionUpdate update in client.CompleteChatStreamingAsync(chatMessages, options, cancellationToken))
        {
            foreach (ChatMessageContentPart part in update.ContentUpdate)
            {
                if (part.Text is not null)
                {
                    yield return part.Text;
                }
            }
        }
    }

    private ChatClient GetChatClient(string? deploymentName)
    {
        var deployment = deploymentName ?? _defaultDeployment;
        return _chatClients.GetOrAdd(deployment, d => _openAiClient.GetChatClient(d));
    }

    private static List<OpenAI.Chat.ChatMessage> MapMessages(IReadOnlyList<ChatMessage> messages)
    {
        var mapped = new List<OpenAI.Chat.ChatMessage>(messages.Count);

        foreach (var message in messages)
        {
            mapped.Add(message.Role switch
            {
                ChatRole.System => new SystemChatMessage(message.Content),
                ChatRole.User => new UserChatMessage(message.Content),
                ChatRole.Assistant => new AssistantChatMessage(message.Content),
                ChatRole.Tool => new ToolChatMessage(message.ToolCallId ?? string.Empty, message.Content),
                _ => new UserChatMessage(message.Content)
            });
        }

        return mapped;
    }

    private static ChatCompletionOptions? BuildOptions(IReadOnlyList<McpToolDefinition>? availableTools)
    {
        if (availableTools is null or { Count: 0 })
        {
            return null;
        }

        var options = new ChatCompletionOptions();

        foreach (var tool in availableTools)
        {
            options.Tools.Add(ChatTool.CreateFunctionTool(
                tool.Name,
                tool.Description,
                tool.InputSchema is not null ? BinaryData.FromString(tool.InputSchema) : null));
        }

        return options;
    }
}
