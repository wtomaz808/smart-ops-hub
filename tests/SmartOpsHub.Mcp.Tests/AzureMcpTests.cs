using System.Text.Json;
using SmartOpsHub.Mcp.Azure.Tools;

namespace SmartOpsHub.Mcp.Tests;

public class AzureToolDefinitionsTests
{
    [Fact]
    public void All_Returns_Five_Tools()
    {
        Assert.Equal(5, AzureToolDefinitions.All.Count);
    }

    [Theory]
    [InlineData("azure_list_resources")]
    [InlineData("azure_get_resource_health")]
    [InlineData("azure_query_logs")]
    [InlineData("azure_get_metrics")]
    [InlineData("azure_list_alerts")]
    public void All_Contains_Tool(string toolName)
    {
        Assert.Contains(AzureToolDefinitions.All, t => t.Name == toolName);
    }

    [Fact]
    public void All_Tools_Have_Valid_Schemas()
    {
        foreach (var tool in AzureToolDefinitions.All)
        {
            Assert.False(string.IsNullOrEmpty(tool.Name));
            Assert.False(string.IsNullOrEmpty(tool.Description));
            Assert.False(string.IsNullOrEmpty(tool.InputSchema));

            var doc = JsonDocument.Parse(tool.InputSchema!);
            Assert.Equal("object", doc.RootElement.GetProperty("type").GetString());
        }
    }

    [Fact]
    public void ListResources_Requires_SubscriptionId()
    {
        var schema = JsonDocument.Parse(AzureToolDefinitions.ListResources.InputSchema!).RootElement;
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("subscriptionId", required);
    }

    [Fact]
    public void QueryLogs_Requires_WorkspaceId_And_Query()
    {
        var schema = JsonDocument.Parse(AzureToolDefinitions.QueryLogs.InputSchema!).RootElement;
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("workspaceId", required);
        Assert.Contains("query", required);
    }

    [Fact]
    public void GetMetrics_Requires_ResourceId_And_MetricName()
    {
        var schema = JsonDocument.Parse(AzureToolDefinitions.GetMetrics.InputSchema!).RootElement;
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("resourceId", required);
        Assert.Contains("metricName", required);
    }
}
