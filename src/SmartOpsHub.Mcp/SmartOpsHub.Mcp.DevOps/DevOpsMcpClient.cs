using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.DevOps.Tools;

namespace SmartOpsHub.Mcp.DevOps;

public sealed partial class DevOpsMcpClient(HttpClient httpClient, ILogger<DevOpsMcpClient> logger) : IMcpClient
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(DevOpsToolDefinitions.All);

    public async Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken cancellationToken = default)
    {
        try
        {
            return toolCall.ToolName switch
            {
                "devops_list_pipelines" => await HandleListPipelinesAsync(toolCall, cancellationToken),
                "devops_trigger_pipeline" => await HandleTriggerPipelineAsync(toolCall, cancellationToken),
                "devops_get_pipeline_status" => await HandleGetPipelineStatusAsync(toolCall, cancellationToken),
                "devops_list_deployments" => await HandleListDeploymentsAsync(toolCall, cancellationToken),
                "devops_get_deployment_logs" => await HandleGetDeploymentLogsAsync(toolCall, cancellationToken),
                _ => new McpToolResult { ToolCallId = toolCall.Id, Content = $"Unknown tool: {toolCall.ToolName}", IsError = true }
            };
        }
        catch (Exception ex)
        {
            LogToolError(ex, toolCall.ToolName);
            return new McpToolResult { ToolCallId = toolCall.Id, Content = $"DevOps API error: {ex.Message}", IsError = true };
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync("_apis/connectiondata", cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogHealthCheckFailed(ex);
            return false;
        }
    }

    private async Task<McpToolResult> HandleListPipelinesAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var org = args.GetProperty("organization").GetString()!;
        var project = args.GetProperty("project").GetString()!;

        var url = $"https://dev.azure.com/{org}/{project}/_apis/pipelines?api-version=7.1";
        var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        LogToolExecuted("devops_list_pipelines", org, project);
        return new McpToolResult { ToolCallId = toolCall.Id, Content = content };
    }

    private async Task<McpToolResult> HandleTriggerPipelineAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var org = args.GetProperty("organization").GetString()!;
        var project = args.GetProperty("project").GetString()!;
        var pipelineId = args.GetProperty("pipelineId").GetInt32();

        var body = new Dictionary<string, object>();
        if (args.TryGetProperty("branch", out var b) && b.ValueKind == JsonValueKind.String)
            body["resources"] = new { repositories = new { self = new { refName = $"refs/heads/{b.GetString()}" } } };

        var url = $"https://dev.azure.com/{org}/{project}/_apis/pipelines/{pipelineId}/runs?api-version=7.1";
        var response = await httpClient.PostAsJsonAsync(url, body, s_jsonOptions, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        LogToolExecuted("devops_trigger_pipeline", org, project);
        return new McpToolResult { ToolCallId = toolCall.Id, Content = content };
    }

    private async Task<McpToolResult> HandleGetPipelineStatusAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var org = args.GetProperty("organization").GetString()!;
        var project = args.GetProperty("project").GetString()!;
        var runId = args.GetProperty("runId").GetInt32();

        var url = $"https://dev.azure.com/{org}/{project}/_apis/build/builds/{runId}?api-version=7.1";
        var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        LogToolExecuted("devops_get_pipeline_status", org, project);
        return new McpToolResult { ToolCallId = toolCall.Id, Content = content };
    }

    private async Task<McpToolResult> HandleListDeploymentsAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var org = args.GetProperty("organization").GetString()!;
        var project = args.GetProperty("project").GetString()!;

        var url = $"https://vsrm.dev.azure.com/{org}/{project}/_apis/release/deployments?api-version=7.1";
        if (args.TryGetProperty("environment", out var env) && env.ValueKind == JsonValueKind.String)
            url += $"&definitionEnvironmentId=0&queryOrder=descending";

        var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        LogToolExecuted("devops_list_deployments", org, project);
        return new McpToolResult { ToolCallId = toolCall.Id, Content = content };
    }

    private async Task<McpToolResult> HandleGetDeploymentLogsAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var org = args.GetProperty("organization").GetString()!;
        var project = args.GetProperty("project").GetString()!;
        var deploymentId = args.GetProperty("deploymentId").GetInt32();

        var url = $"https://dev.azure.com/{org}/{project}/_apis/build/builds/{deploymentId}/logs?api-version=7.1";
        var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        LogToolExecuted("devops_get_deployment_logs", org, project);
        return new McpToolResult { ToolCallId = toolCall.Id, Content = content };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executed {ToolName} for {Org}/{Project}")]
    private partial void LogToolExecuted(string toolName, string org, string project);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error executing DevOps tool {ToolName}")]
    private partial void LogToolError(Exception ex, string toolName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "DevOps health check failed")]
    private partial void LogHealthCheckFailed(Exception ex);
}
