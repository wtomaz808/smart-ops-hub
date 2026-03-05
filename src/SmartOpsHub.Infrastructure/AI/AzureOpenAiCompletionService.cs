using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
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
    private readonly AzureOpenAIClient? _openAiClient;
    private readonly string _defaultDeployment;
    private readonly ConcurrentDictionary<string, ChatClient> _chatClients = new();
    private readonly bool _isConfigured;

    public AzureOpenAiCompletionService(IConfiguration configuration)
    {
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        _defaultDeployment = configuration["AzureOpenAI:DeploymentName"]
            ?? ModelOption.Default.DeploymentName;

        if (string.IsNullOrEmpty(endpoint) || endpoint.Contains("YOUR_RESOURCE"))
        {
            _isConfigured = false;
            return;
        }

        _isConfigured = true;
        var credential = new DefaultAzureCredential();

        // Detect Azure Gov endpoints and set the correct token audience
        var clientOptions = new AzureOpenAIClientOptions();
        if (endpoint.Contains(".azure.us", StringComparison.OrdinalIgnoreCase))
        {
            clientOptions.Audience = AzureOpenAIAudience.AzureGovernment;
        }

        _openAiClient = new AzureOpenAIClient(new Uri(endpoint), credential, clientOptions);
    }

    public async Task<string> GetCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<McpToolDefinition>? availableTools = null,
        string? deploymentName = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
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
        EnsureConfigured();
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

    public async IAsyncEnumerable<CompletionStreamEvent> StreamWithToolDetectionAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<McpToolDefinition>? availableTools = null,
        string? deploymentName = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var client = GetChatClient(deploymentName);
        var chatMessages = MapMessages(messages);
        var options = BuildOptions(availableTools);

        // Accumulate tool call parts across streaming updates
        var toolCallIdsByIndex = new Dictionary<int, string>();
        var toolCallNamesByIndex = new Dictionary<int, string>();
        var toolCallArgsByIndex = new Dictionary<int, StringBuilder>();

        await foreach (StreamingChatCompletionUpdate update in client.CompleteChatStreamingAsync(chatMessages, options, cancellationToken))
        {
            foreach (ChatMessageContentPart part in update.ContentUpdate)
            {
                if (part.Text is not null)
                {
                    yield return new TextTokenEvent(part.Text);
                }
            }

            foreach (StreamingChatToolCallUpdate toolUpdate in update.ToolCallUpdates)
            {
                if (toolUpdate.ToolCallId is not null)
                {
                    toolCallIdsByIndex[toolUpdate.Index] = toolUpdate.ToolCallId;
                }
                if (toolUpdate.FunctionName is not null)
                {
                    toolCallNamesByIndex[toolUpdate.Index] = toolUpdate.FunctionName;
                }
                if (toolUpdate.FunctionArgumentsUpdate is not null)
                {
                    if (!toolCallArgsByIndex.TryGetValue(toolUpdate.Index, out var sb))
                    {
                        sb = new StringBuilder();
                        toolCallArgsByIndex[toolUpdate.Index] = sb;
                    }
                    sb.Append(toolUpdate.FunctionArgumentsUpdate);
                }
            }
        }

        // If tool calls were accumulated, emit them
        if (toolCallIdsByIndex.Count > 0)
        {
            var toolCalls = toolCallIdsByIndex.Keys
                .OrderBy(i => i)
                .Select(i => new AiToolCallRequest
                {
                    Id = toolCallIdsByIndex[i],
                    FunctionName = toolCallNamesByIndex.GetValueOrDefault(i, ""),
                    Arguments = toolCallArgsByIndex.TryGetValue(i, out var sb) ? sb.ToString() : "{}"
                })
                .ToList();

            yield return new ToolCallsCompleteEvent(toolCalls);
        }
    }

    private ChatClient GetChatClient(string? deploymentName)
    {
        var deployment = deploymentName ?? _defaultDeployment;
        return _chatClients.GetOrAdd(deployment, d => _openAiClient!.GetChatClient(d));
    }

    private void EnsureConfigured()
    {
        if (!_isConfigured)
        {
            throw new InvalidOperationException(
                "Azure OpenAI is not configured. Set the AzureOpenAI:Endpoint configuration value.");
        }
    }

    private static List<OpenAI.Chat.ChatMessage> MapMessages(IReadOnlyList<ChatMessage> messages)
    {
        var mapped = new List<OpenAI.Chat.ChatMessage>(messages.Count);

        foreach (var message in messages)
        {
            if (message.Role == ChatRole.Assistant && message.ToolCalls.Count > 0)
            {
                // Assistant message that requested tool calls
                var assistantMsg = new AssistantChatMessage(message.Content);
                foreach (var tc in message.ToolCalls)
                {
                    assistantMsg.ToolCalls.Add(ChatToolCall.CreateFunctionToolCall(
                        tc.Id, tc.FunctionName, BinaryData.FromString(tc.Arguments)));
                }
                mapped.Add(assistantMsg);
            }
            else
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
