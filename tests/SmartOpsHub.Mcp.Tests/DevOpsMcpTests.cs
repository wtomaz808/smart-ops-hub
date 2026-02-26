using System.Text.Json;
using SmartOpsHub.Mcp.DevOps.Tools;

namespace SmartOpsHub.Mcp.Tests;

public class DevOpsToolDefinitionsTests
{
    [Fact]
    public void All_Returns_Five_Tools()
    {
        Assert.Equal(5, DevOpsToolDefinitions.All.Count);
    }

    [Theory]
    [InlineData("devops_list_pipelines")]
    [InlineData("devops_trigger_pipeline")]
    [InlineData("devops_get_pipeline_status")]
    [InlineData("devops_list_deployments")]
    [InlineData("devops_get_deployment_logs")]
    public void All_Contains_Tool(string toolName)
    {
        Assert.Contains(DevOpsToolDefinitions.All, t => t.Name == toolName);
    }

    [Fact]
    public void All_Tools_Have_Valid_Schemas()
    {
        foreach (var tool in DevOpsToolDefinitions.All)
        {
            Assert.False(string.IsNullOrEmpty(tool.Name));
            Assert.False(string.IsNullOrEmpty(tool.Description));
            var doc = JsonDocument.Parse(tool.InputSchema!);
            Assert.Equal("object", doc.RootElement.GetProperty("type").GetString());
        }
    }

    [Fact]
    public void TriggerPipeline_Requires_PipelineId()
    {
        var schema = JsonDocument.Parse(DevOpsToolDefinitions.TriggerPipeline.InputSchema!).RootElement;
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("pipelineId", required);
    }

    [Fact]
    public void GetPipelineStatus_Requires_RunId()
    {
        var schema = JsonDocument.Parse(DevOpsToolDefinitions.GetPipelineStatus.InputSchema!).RootElement;
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("runId", required);
    }
}
