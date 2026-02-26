using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using OpenAI.Embeddings;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.AI.Tools;

namespace SmartOpsHub.Mcp.AI;

public sealed partial class AiMcpClient(AzureOpenAIClient openAiClient, ILogger<AiMcpClient> logger) : IMcpClient
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(AiToolDefinitions.All);

    public async Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken cancellationToken = default)
    {
        try
        {
            return toolCall.ToolName switch
            {
                "ai_generate_completion" => await HandleGenerateCompletionAsync(toolCall, cancellationToken),
                "ai_generate_embedding" => await HandleGenerateEmbeddingAsync(toolCall, cancellationToken),
                "ai_analyze_image" => await HandleAnalyzeImageAsync(toolCall, cancellationToken),
                "ai_transcribe_speech" => await HandleTranscribeSpeechAsync(toolCall, cancellationToken),
                "ai_analyze_text" => await HandleAnalyzeTextAsync(toolCall, cancellationToken),
                _ => new McpToolResult { ToolCallId = toolCall.Id, Content = $"Unknown tool: {toolCall.ToolName}", IsError = true }
            };
        }
        catch (Exception ex)
        {
            LogToolError(ex, toolCall.ToolName);
            return new McpToolResult { ToolCallId = toolCall.Id, Content = $"AI service error: {ex.Message}", IsError = true };
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var chatClient = openAiClient.GetChatClient("gpt-4o");
            var response = await chatClient.CompleteChatAsync(
                [new SystemChatMessage("health check")],
                new ChatCompletionOptions { MaxOutputTokenCount = 1 },
                cancellationToken).ConfigureAwait(false);
            return response.Value is not null;
        }
        catch (Exception ex)
        {
            LogHealthCheckFailed(ex);
            return false;
        }
    }

    private async Task<McpToolResult> HandleGenerateCompletionAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var prompt = args.GetProperty("prompt").GetString()!;
        var model = args.TryGetProperty("model", out var m) && m.ValueKind == JsonValueKind.String
            ? m.GetString()! : "gpt-4o";

        var options = new ChatCompletionOptions();
        if (args.TryGetProperty("maxTokens", out var mt) && mt.ValueKind == JsonValueKind.Number)
            options.MaxOutputTokenCount = mt.GetInt32();
        if (args.TryGetProperty("temperature", out var temp) && temp.ValueKind == JsonValueKind.Number)
            options.Temperature = (float)temp.GetDouble();

        var chatClient = openAiClient.GetChatClient(model);
        var response = await chatClient.CompleteChatAsync(
            [new UserChatMessage(prompt)], options, ct).ConfigureAwait(false);

        var result = response.Value;
        LogToolExecuted("ai_generate_completion", model);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = JsonSerializer.Serialize(new
            {
                Text = result.Content[0].Text,
                Model = model,
                Usage = new
                {
                    PromptTokens = result.Usage.InputTokenCount,
                    CompletionTokens = result.Usage.OutputTokenCount
                }
            }, s_jsonOptions)
        };
    }

    private async Task<McpToolResult> HandleGenerateEmbeddingAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var text = args.GetProperty("text").GetString()!;
        var model = args.TryGetProperty("model", out var m) && m.ValueKind == JsonValueKind.String
            ? m.GetString()! : "text-embedding-ada-002";

        var embeddingClient = openAiClient.GetEmbeddingClient(model);
        var response = await embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: ct).ConfigureAwait(false);

        var embedding = response.Value;
        LogToolExecuted("ai_generate_embedding", model);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = JsonSerializer.Serialize(new
            {
                Embedding = embedding.ToFloats().ToArray(),
                Dimensions = embedding.ToFloats().Length
            }, s_jsonOptions)
        };
    }

    private async Task<McpToolResult> HandleAnalyzeImageAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var imageUrl = args.GetProperty("imageUrl").GetString()!;
        var prompt = args.TryGetProperty("prompt", out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString()! : "Describe this image in detail.";

        var chatClient = openAiClient.GetChatClient("gpt-4o");
        var imageContent = ChatMessageContentPart.CreateImagePart(new Uri(imageUrl));
        var textContent = ChatMessageContentPart.CreateTextPart(prompt);

        var response = await chatClient.CompleteChatAsync(
            [new UserChatMessage(textContent, imageContent)],
            cancellationToken: ct).ConfigureAwait(false);

        LogToolExecuted("ai_analyze_image", "gpt-4o");
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = JsonSerializer.Serialize(new
            {
                Description = response.Value.Content[0].Text
            }, s_jsonOptions)
        };
    }

    private Task<McpToolResult> HandleTranscribeSpeechAsync(McpToolCall toolCall, CancellationToken ct)
    {
        // Azure AI Speech SDK requires separate package; return not-implemented for now
        LogToolExecuted("ai_transcribe_speech", "whisper");
        return Task.FromResult(new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = JsonSerializer.Serialize(new
            {
                Error = "Speech transcription requires Azure AI Speech SDK configuration. Please configure the speech endpoint.",
                Supported = false
            }, s_jsonOptions),
            IsError = true
        });
    }

    private async Task<McpToolResult> HandleAnalyzeTextAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var text = args.GetProperty("text").GetString()!;
        var analysisType = args.TryGetProperty("analysisType", out var at) && at.ValueKind == JsonValueKind.String
            ? at.GetString()! : "all";

        // Use GPT-4o for text analysis as a unified approach
        var prompt = analysisType switch
        {
            "sentiment" => $"Analyze the sentiment of this text. Return JSON with 'sentiment' (positive/negative/neutral) and 'confidence' (0-1):\n\n{text}",
            "entities" => $"Extract named entities from this text. Return JSON with 'entities' array, each having 'text', 'type', and 'confidence':\n\n{text}",
            "keyPhrases" => $"Extract key phrases from this text. Return JSON with 'keyPhrases' array of strings:\n\n{text}",
            _ => $"Analyze this text. Return JSON with 'sentiment' (positive/negative/neutral), 'confidence' (0-1), 'keyPhrases' (array of strings), and 'entities' (array with 'text' and 'type'):\n\n{text}"
        };

        var chatClient = openAiClient.GetChatClient("gpt-4o");
        var response = await chatClient.CompleteChatAsync(
            [new SystemChatMessage("You are a text analysis assistant. Always respond with valid JSON only."),
             new UserChatMessage(prompt)],
            new ChatCompletionOptions { Temperature = 0 },
            ct).ConfigureAwait(false);

        LogToolExecuted("ai_analyze_text", "gpt-4o");
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = response.Value.Content[0].Text
        };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executed {ToolName} using model {Model}")]
    private partial void LogToolExecuted(string toolName, string model);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error executing AI tool {ToolName}")]
    private partial void LogToolError(Exception ex, string toolName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "AI service health check failed")]
    private partial void LogHealthCheckFailed(Exception ex);
}
