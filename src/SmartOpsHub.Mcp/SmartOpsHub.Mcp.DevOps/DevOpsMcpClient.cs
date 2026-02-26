using System.Text.Json;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.DevOps.Tools;

namespace SmartOpsHub.Mcp.DevOps;

public sealed class DevOpsMcpClient : IMcpClient
{
    private readonly HttpClient _httpClient;

    public DevOpsMcpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(DevOpsToolDefinitions.All);

    public Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken cancellationToken = default)
    {
        var result = toolCall.ToolName switch
        {
            "devops_list_pipelines" => Stub(toolCall, new[] { new { id = 1, name = "CI Pipeline", status = "enabled" } }),
            "devops_trigger_pipeline" => Stub(toolCall, new { runId = 100, status = "queued", pipeline = "CI Pipeline" }),
            "devops_get_pipeline_status" => Stub(toolCall, new { runId = 100, status = "succeeded", duration = "00:05:30" }),
            "devops_list_deployments" => Stub(toolCall, new[] { new { id = 1, environment = "production", status = "succeeded" } }),
            "devops_get_deployment_logs" => Stub(toolCall, new { deploymentId = 1, logs = "Deployment completed successfully." }),
            _ => new McpToolResult { ToolCallId = toolCall.Id, Content = $"Unknown tool: {toolCall.ToolName}", IsError = true }
        };

        return Task.FromResult(result);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    private static McpToolResult Stub(McpToolCall toolCall, object data)
        => new() { ToolCallId = toolCall.Id, Content = JsonSerializer.Serialize(data) };
}
