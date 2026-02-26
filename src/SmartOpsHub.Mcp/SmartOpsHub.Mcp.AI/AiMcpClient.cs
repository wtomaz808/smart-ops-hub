using System.Text.Json;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.AI.Tools;

namespace SmartOpsHub.Mcp.AI;

public sealed class AiMcpClient : IMcpClient
{
    private static readonly double[] StubEmbedding = [0.1, 0.2, 0.3];
    private static readonly string[] StubKeyPhrases = ["stub", "analysis"];

    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(AiToolDefinitions.All);

    public Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken cancellationToken = default)
    {
        var result = toolCall.ToolName switch
        {
            "ai_generate_completion" => Stub(toolCall, new { text = "This is a stub completion response.", model = "gpt-4", usage = new { promptTokens = 10, completionTokens = 20 } }),
            "ai_generate_embedding" => Stub(toolCall, new { embedding = StubEmbedding, dimensions = 3 }),
            "ai_analyze_image" => Stub(toolCall, new { description = "Stub image analysis result", confidence = 0.95 }),
            "ai_transcribe_speech" => Stub(toolCall, new { text = "Stub transcription result", language = "en", duration = "00:01:30" }),
            "ai_analyze_text" => Stub(toolCall, new { sentiment = "positive", confidence = 0.87, keyPhrases = StubKeyPhrases }),
            _ => new McpToolResult { ToolCallId = toolCall.Id, Content = $"Unknown tool: {toolCall.ToolName}", IsError = true }
        };

        return Task.FromResult(result);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    private static McpToolResult Stub(McpToolCall toolCall, object data)
        => new() { ToolCallId = toolCall.Id, Content = JsonSerializer.Serialize(data) };
}
