using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Logging;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.Azure.Tools;

namespace SmartOpsHub.Mcp.Azure;

public sealed partial class AzureMcpClient(
    ArmClient armClient,
    LogsQueryClient logsClient,
    MetricsQueryClient metricsClient,
    ILogger<AzureMcpClient> logger) : IMcpClient
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(AzureToolDefinitions.All);

    public async Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken cancellationToken = default)
    {
        try
        {
            return toolCall.ToolName switch
            {
                "azure_list_resources" => await HandleListResourcesAsync(toolCall, cancellationToken),
                "azure_get_resource_health" => await HandleGetResourceHealthAsync(toolCall, cancellationToken),
                "azure_query_logs" => await HandleQueryLogsAsync(toolCall, cancellationToken),
                "azure_get_metrics" => await HandleGetMetricsAsync(toolCall, cancellationToken),
                "azure_list_alerts" => await HandleListAlertsAsync(toolCall, cancellationToken),
                _ => new McpToolResult { ToolCallId = toolCall.Id, Content = $"Unknown tool: {toolCall.ToolName}", IsError = true }
            };
        }
        catch (Exception ex)
        {
            LogToolError(ex, toolCall.ToolName);
            return new McpToolResult { ToolCallId = toolCall.Id, Content = $"Azure API error: {ex.Message}", IsError = true };
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await foreach (var _ in armClient.GetSubscriptions().GetAllAsync(cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
            return true;
        }
        catch (Exception ex)
        {
            LogHealthCheckFailed(ex);
            return false;
        }
    }

    private async Task<McpToolResult> HandleListResourcesAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var subscriptionId = args.GetProperty("subscriptionId").GetString()!;

        var subscription = armClient.GetSubscriptionResource(
            SubscriptionResource.CreateResourceIdentifier(subscriptionId));

        var resources = new List<object>();
        var query = subscription.GetGenericResourcesAsync(cancellationToken: ct);

        if (args.TryGetProperty("resourceGroup", out var rgEl) && rgEl.ValueKind == JsonValueKind.String)
        {
            var rgName = rgEl.GetString()!;
            var rg = armClient.GetResourceGroupResource(
                ResourceGroupResource.CreateResourceIdentifier(subscriptionId, rgName));
            query = rg.GetGenericResourcesAsync(cancellationToken: ct);
        }

        await foreach (var resource in query.ConfigureAwait(false))
        {
            resources.Add(new
            {
                Name = resource.Data.Name,
                Type = resource.Data.ResourceType.ToString(),
                Location = resource.Data.Location.Name,
                ProvisioningState = resource.Data.ProvisioningState
            });
            if (resources.Count >= 100) break; // Cap results
        }

        LogToolExecuted("azure_list_resources", subscriptionId);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = JsonSerializer.Serialize(resources, s_jsonOptions)
        };
    }

    private async Task<McpToolResult> HandleGetResourceHealthAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var resourceId = args.GetProperty("resourceId").GetString()!;

        var resource = armClient.GetGenericResource(new ResourceIdentifier(resourceId));
        var data = (await resource.GetAsync(ct).ConfigureAwait(false)).Value.Data;

        LogToolExecuted("azure_get_resource_health", resourceId);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = JsonSerializer.Serialize(new
            {
                ResourceId = resourceId,
                Name = data.Name,
                Type = data.ResourceType.ToString(),
                ProvisioningState = data.ProvisioningState,
                Location = data.Location.Name
            }, s_jsonOptions)
        };
    }

    private async Task<McpToolResult> HandleQueryLogsAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var workspaceId = args.GetProperty("workspaceId").GetString()!;
        var query = args.GetProperty("query").GetString()!;

        var timespan = QueryTimeRange.All;
        if (args.TryGetProperty("timespan", out var ts) && ts.ValueKind == JsonValueKind.String)
        {
            if (TimeSpan.TryParse(ts.GetString(), out var parsed))
                timespan = new QueryTimeRange(parsed);
        }

        var response = await logsClient.QueryWorkspaceAsync(workspaceId, query, timespan, cancellationToken: ct).ConfigureAwait(false);
        var table = response.Value.Table;

        var rows = new List<Dictionary<string, object?>>();
        foreach (var row in table.Rows)
        {
            var dict = new Dictionary<string, object?>();
            for (var i = 0; i < table.Columns.Count; i++)
                dict[table.Columns[i].Name] = row[i];
            rows.Add(dict);
            if (rows.Count >= 100) break;
        }

        LogToolExecuted("azure_query_logs", workspaceId);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = JsonSerializer.Serialize(new { Count = rows.Count, Rows = rows }, s_jsonOptions)
        };
    }

    private async Task<McpToolResult> HandleGetMetricsAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var resourceId = args.GetProperty("resourceId").GetString()!;
        var metricName = args.GetProperty("metricName").GetString()!;

        var options = new MetricsQueryOptions();
        if (args.TryGetProperty("timespan", out var ts) && ts.ValueKind == JsonValueKind.String)
        {
            if (TimeSpan.TryParse(ts.GetString(), out var parsed))
                options.TimeRange = new QueryTimeRange(parsed);
        }

        var response = await metricsClient.QueryResourceAsync(
            new ResourceIdentifier(resourceId), [metricName], options, ct).ConfigureAwait(false);

        var metricResults = new List<object>();
        foreach (var metric in response.Value.Metrics)
        {
            foreach (var ts2 in metric.TimeSeries)
            {
                foreach (var val in ts2.Values)
                {
                    metricResults.Add(new
                    {
                        Metric = metric.Name,
                        Timestamp = val.TimeStamp,
                        val.Average, val.Minimum, val.Maximum, val.Total, val.Count
                    });
                }
            }
        }

        LogToolExecuted("azure_get_metrics", resourceId);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = JsonSerializer.Serialize(metricResults, s_jsonOptions)
        };
    }

    private async Task<McpToolResult> HandleListAlertsAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var subscriptionId = args.GetProperty("subscriptionId").GetString()!;

        // Use Azure Resource Graph to query alerts via ARM
        var subscription = armClient.GetSubscriptionResource(
            SubscriptionResource.CreateResourceIdentifier(subscriptionId));

        var alerts = new List<object>();
        var query = subscription.GetGenericResourcesAsync(
            filter: "resourceType eq 'Microsoft.AlertsManagement/alerts'",
            cancellationToken: ct);

        await foreach (var alert in query.ConfigureAwait(false))
        {
            alerts.Add(new
            {
                Name = alert.Data.Name,
                Type = alert.Data.ResourceType.ToString(),
                Location = alert.Data.Location.Name,
                ProvisioningState = alert.Data.ProvisioningState
            });
            if (alerts.Count >= 50) break;
        }

        LogToolExecuted("azure_list_alerts", subscriptionId);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = JsonSerializer.Serialize(alerts, s_jsonOptions)
        };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executed {ToolName} for {ResourceScope}")]
    private partial void LogToolExecuted(string toolName, string resourceScope);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error executing Azure tool {ToolName}")]
    private partial void LogToolError(Exception ex, string toolName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Azure health check failed")]
    private partial void LogHealthCheckFailed(Exception ex);
}
