using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Mcp.Azure.Tools;

public static class AzureToolDefinitions
{
    public static readonly McpToolDefinition ListResources = new()
    {
        Name = "azure_list_resources",
        Description = "Lists Azure resources in a subscription or resource group",
        InputSchema = """{"type":"object","properties":{"subscriptionId":{"type":"string"},"resourceGroup":{"type":"string"}},"required":["subscriptionId"]}"""
    };

    public static readonly McpToolDefinition GetResourceHealth = new()
    {
        Name = "azure_get_resource_health",
        Description = "Gets the health status of an Azure resource",
        InputSchema = """{"type":"object","properties":{"resourceId":{"type":"string"}},"required":["resourceId"]}"""
    };

    public static readonly McpToolDefinition QueryLogs = new()
    {
        Name = "azure_query_logs",
        Description = "Queries Azure Monitor logs using KQL",
        InputSchema = """{"type":"object","properties":{"workspaceId":{"type":"string"},"query":{"type":"string"},"timespan":{"type":"string"}},"required":["workspaceId","query"]}"""
    };

    public static readonly McpToolDefinition GetMetrics = new()
    {
        Name = "azure_get_metrics",
        Description = "Gets metrics for an Azure resource",
        InputSchema = """{"type":"object","properties":{"resourceId":{"type":"string"},"metricName":{"type":"string"},"timespan":{"type":"string"}},"required":["resourceId","metricName"]}"""
    };

    public static readonly McpToolDefinition ListAlerts = new()
    {
        Name = "azure_list_alerts",
        Description = "Lists active Azure Monitor alerts",
        InputSchema = """{"type":"object","properties":{"subscriptionId":{"type":"string"},"severity":{"type":"string"}},"required":["subscriptionId"]}"""
    };

    public static IReadOnlyList<McpToolDefinition> All =>
    [
        ListResources, GetResourceHealth, QueryLogs, GetMetrics, ListAlerts
    ];
}
