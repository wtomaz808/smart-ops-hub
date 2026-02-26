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
    private readonly ChatClient _chatClient;

    public AzureOpenAiCompletionService(IConfiguration configuration)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint configuration is required.");
        var deploymentName = configuration["AzureOpenAI:DeploymentName"]
            ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName configuration is required.");

        var credential = new DefaultAzureCredential();
        var client = new AzureOpenAIClient(new Uri(endpoint), credential);
        _chatClient = client.GetChatClient(deploymentName);
    }

    public async Task<string> GetCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<McpToolDefinition>? availableTools = null,
        CancellationToken cancellationToken = default)
    {
        var chatMessages = MapMessages(messages);
        var options = BuildOptions(availableTools);

        ChatCompletion completion = await _chatClient.CompleteChatAsync(chatMessages, options, cancellationToken);

        return completion.Content[0].Text ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<McpToolDefinition>? availableTools = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatMessages = MapMessages(messages);
        var options = BuildOptions(availableTools);

        await foreach (StreamingChatCompletionUpdate update in _chatClient.CompleteChatStreamingAsync(chatMessages, options, cancellationToken))
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
