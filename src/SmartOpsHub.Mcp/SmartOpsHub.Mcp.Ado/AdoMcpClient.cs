using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.Ado.Tools;

namespace SmartOpsHub.Mcp.Ado;

public sealed partial class AdoMcpClient(HttpClient httpClient, ILogger<AdoMcpClient> logger) : IMcpClient
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(AdoToolDefinitions.All);

    public async Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken cancellationToken = default)
    {
        try
        {
            return toolCall.ToolName switch
            {
                "ado_create_epic" => await HandleCreateWorkItemAsync(toolCall, "Epic", cancellationToken),
                "ado_create_work_item" => await HandleCreateWorkItemAsync(toolCall, null, cancellationToken),
                "ado_list_work_items" => await HandleListWorkItemsAsync(toolCall, cancellationToken),
                "ado_get_sprint" => await HandleGetSprintAsync(toolCall, cancellationToken),
                "ado_update_work_item_state" => await HandleUpdateWorkItemStateAsync(toolCall, cancellationToken),
                "ado_query_work_items" => await HandleQueryWorkItemsAsync(toolCall, cancellationToken),
                _ => new McpToolResult { ToolCallId = toolCall.Id, Content = $"Unknown tool: {toolCall.ToolName}", IsError = true }
            };
        }
        catch (Exception ex)
        {
            LogToolError(ex, toolCall.ToolName);
            return new McpToolResult { ToolCallId = toolCall.Id, Content = $"Azure DevOps API error: {ex.Message}", IsError = true };
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

    private async Task<McpToolResult> HandleCreateWorkItemAsync(McpToolCall toolCall, string? forceType, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var org = args.GetProperty("organization").GetString()!;
        var project = args.GetProperty("project").GetString()!;
        var title = args.GetProperty("title").GetString()!;
        var type = forceType ?? args.GetProperty("type").GetString()!;

        var patchDoc = new List<object>
        {
            new { op = "add", path = "/fields/System.Title", value = title }
        };

        if (args.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.String)
            patchDoc.Add(new { op = "add", path = "/fields/System.Description", value = desc.GetString() });

        var url = $"https://dev.azure.com/{org}/{project}/_apis/wit/workitems/${type}?api-version=7.1";
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(patchDoc, options: s_jsonOptions)
        };
        request.Content.Headers.ContentType!.MediaType = "application/json-patch+json";

        var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        LogToolExecuted("ado_create_work_item", org, project);
        return new McpToolResult { ToolCallId = toolCall.Id, Content = content };
    }

    private async Task<McpToolResult> HandleListWorkItemsAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var org = args.GetProperty("organization").GetString()!;
        var project = args.GetProperty("project").GetString()!;

        var wiql = "SELECT [System.Id], [System.Title], [System.State], [System.WorkItemType] FROM workitems WHERE [System.TeamProject] = @project";
        if (args.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String)
            wiql += $" AND [System.WorkItemType] = '{typeEl.GetString()}'";
        if (args.TryGetProperty("state", out var stateEl) && stateEl.ValueKind == JsonValueKind.String)
            wiql += $" AND [System.State] = '{stateEl.GetString()}'";
        wiql += " ORDER BY [System.ChangedDate] DESC";

        var url = $"https://dev.azure.com/{org}/{project}/_apis/wit/wiql?api-version=7.1";
        var response = await httpClient.PostAsJsonAsync(url, new { query = wiql }, s_jsonOptions, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        LogToolExecuted("ado_list_work_items", org, project);
        return new McpToolResult { ToolCallId = toolCall.Id, Content = content };
    }

    private async Task<McpToolResult> HandleGetSprintAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var org = args.GetProperty("organization").GetString()!;
        var project = args.GetProperty("project").GetString()!;
        var team = args.TryGetProperty("team", out var t) && t.ValueKind == JsonValueKind.String
            ? t.GetString()! : $"{project} Team";

        var url = $"https://dev.azure.com/{org}/{project}/{team}/_apis/work/teamsettings/iterations?$timeframe=current&api-version=7.1";
        var response = await httpClient.GetAsync(url, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        LogToolExecuted("ado_get_sprint", org, project);
        return new McpToolResult { ToolCallId = toolCall.Id, Content = content };
    }

    private async Task<McpToolResult> HandleUpdateWorkItemStateAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var org = args.GetProperty("organization").GetString()!;
        var project = args.GetProperty("project").GetString()!;
        var workItemId = args.GetProperty("workItemId").GetInt32();
        var state = args.GetProperty("state").GetString()!;

        var patchDoc = new[] { new { op = "add", path = "/fields/System.State", value = state } };
        var url = $"https://dev.azure.com/{org}/{project}/_apis/wit/workitems/{workItemId}?api-version=7.1";

        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(patchDoc, options: s_jsonOptions)
        };
        request.Content.Headers.ContentType!.MediaType = "application/json-patch+json";

        var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        LogToolExecuted("ado_update_work_item_state", org, project);
        return new McpToolResult { ToolCallId = toolCall.Id, Content = content };
    }

    private async Task<McpToolResult> HandleQueryWorkItemsAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var org = args.GetProperty("organization").GetString()!;
        var project = args.GetProperty("project").GetString()!;
        var query = args.GetProperty("query").GetString()!;

        var url = $"https://dev.azure.com/{org}/{project}/_apis/wit/wiql?api-version=7.1";
        var response = await httpClient.PostAsJsonAsync(url, new { query }, s_jsonOptions, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        LogToolExecuted("ado_query_work_items", org, project);
        return new McpToolResult { ToolCallId = toolCall.Id, Content = content };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executed {ToolName} for {Org}/{Project}")]
    private partial void LogToolExecuted(string toolName, string org, string project);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error executing ADO tool {ToolName}")]
    private partial void LogToolError(Exception ex, string toolName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Azure DevOps health check failed")]
    private partial void LogHealthCheckFailed(Exception ex);
}
