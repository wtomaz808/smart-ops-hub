using System.Text.Json;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.Ado.Tools;

namespace SmartOpsHub.Mcp.Ado;

public sealed class AdoMcpClient : IMcpClient
{
    private readonly HttpClient _httpClient;

    public AdoMcpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(AdoToolDefinitions.All);

    public Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken cancellationToken = default)
    {
        var result = toolCall.ToolName switch
        {
            "ado_create_epic" => Stub(toolCall, new { id = 1, type = "Epic", title = "Stub Epic", state = "New" }),
            "ado_create_work_item" => Stub(toolCall, new { id = 2, type = "Task", title = "Stub Work Item", state = "New" }),
            "ado_list_work_items" => Stub(toolCall, new[] { new { id = 1, type = "Task", title = "Sample", state = "Active" } }),
            "ado_get_sprint" => Stub(toolCall, new { name = "Sprint 1", startDate = "2024-01-01", endDate = "2024-01-14" }),
            "ado_update_work_item_state" => Stub(toolCall, new { id = 1, state = "Done", updated = true }),
            "ado_query_work_items" => Stub(toolCall, new { count = 0, workItems = Array.Empty<object>() }),
            _ => new McpToolResult { ToolCallId = toolCall.Id, Content = $"Unknown tool: {toolCall.ToolName}", IsError = true }
        };

        return Task.FromResult(result);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    private static McpToolResult Stub(McpToolCall toolCall, object data)
        => new() { ToolCallId = toolCall.Id, Content = JsonSerializer.Serialize(data) };
}
