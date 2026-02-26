using System.Text.Json;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.Azure.Tools;

namespace SmartOpsHub.Mcp.Azure;

public sealed class AzureMcpClient : IMcpClient
{
    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(AzureToolDefinitions.All);

    public Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken cancellationToken = default)
    {
        var result = toolCall.ToolName switch
        {
            "azure_list_resources" => Stub(toolCall, new[] { new { name = "vm-01", type = "Microsoft.Compute/virtualMachines", status = "Running" } }),
            "azure_get_resource_health" => Stub(toolCall, new { status = "Healthy", details = "All checks passed" }),
            "azure_query_logs" => Stub(toolCall, new { rows = Array.Empty<object>(), count = 0 }),
            "azure_get_metrics" => Stub(toolCall, new { metric = "CpuPercentage", average = 45.2, unit = "Percent" }),
            "azure_list_alerts" => Stub(toolCall, new[] { new { name = "High CPU", severity = "Sev2", status = "Fired" } }),
            _ => new McpToolResult { ToolCallId = toolCall.Id, Content = $"Unknown tool: {toolCall.ToolName}", IsError = true }
        };

        return Task.FromResult(result);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    private static McpToolResult Stub(McpToolCall toolCall, object data)
        => new() { ToolCallId = toolCall.Id, Content = JsonSerializer.Serialize(data) };
}
