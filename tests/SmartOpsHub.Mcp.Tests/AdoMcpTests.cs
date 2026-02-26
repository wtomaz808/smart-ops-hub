using System.Text.Json;
using SmartOpsHub.Mcp.Ado.Tools;

namespace SmartOpsHub.Mcp.Tests;

public class AdoToolDefinitionsTests
{
    [Fact]
    public void All_Returns_Six_Tools()
    {
        Assert.Equal(6, AdoToolDefinitions.All.Count);
    }

    [Theory]
    [InlineData("ado_create_epic")]
    [InlineData("ado_create_work_item")]
    [InlineData("ado_list_work_items")]
    [InlineData("ado_get_sprint")]
    [InlineData("ado_update_work_item_state")]
    [InlineData("ado_query_work_items")]
    public void All_Contains_Tool(string toolName)
    {
        Assert.Contains(AdoToolDefinitions.All, t => t.Name == toolName);
    }

    [Fact]
    public void All_Tools_Have_Valid_Schemas()
    {
        foreach (var tool in AdoToolDefinitions.All)
        {
            Assert.False(string.IsNullOrEmpty(tool.Name));
            Assert.False(string.IsNullOrEmpty(tool.Description));
            var doc = JsonDocument.Parse(tool.InputSchema!);
            Assert.Equal("object", doc.RootElement.GetProperty("type").GetString());
        }
    }

    [Fact]
    public void CreateEpic_Requires_Organization_Project_Title()
    {
        var schema = JsonDocument.Parse(AdoToolDefinitions.CreateEpic.InputSchema!).RootElement;
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("organization", required);
        Assert.Contains("project", required);
        Assert.Contains("title", required);
    }

    [Fact]
    public void CreateWorkItem_Requires_Type()
    {
        var schema = JsonDocument.Parse(AdoToolDefinitions.CreateWorkItem.InputSchema!).RootElement;
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("type", required);
    }

    [Fact]
    public void UpdateWorkItemState_Requires_WorkItemId_And_State()
    {
        var schema = JsonDocument.Parse(AdoToolDefinitions.UpdateWorkItemState.InputSchema!).RootElement;
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("workItemId", required);
        Assert.Contains("state", required);
    }
}
