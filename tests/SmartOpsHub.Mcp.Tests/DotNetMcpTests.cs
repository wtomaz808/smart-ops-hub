using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.DotNet;
using SmartOpsHub.Mcp.DotNet.Tools;

namespace SmartOpsHub.Mcp.Tests;

public class DotNetToolDefinitionsTests
{
    [Fact]
    public void All_Returns_Five_Tools()
    {
        Assert.Equal(5, DotNetToolDefinitions.All.Count);
    }

    [Theory]
    [InlineData("dotnet_build_project")]
    [InlineData("dotnet_run_tests")]
    [InlineData("dotnet_list_packages")]
    [InlineData("dotnet_analyze_code")]
    [InlineData("dotnet_add_package")]
    public void All_Contains_Tool(string toolName)
    {
        Assert.Contains(DotNetToolDefinitions.All, t => t.Name == toolName);
    }

    [Fact]
    public void All_Tools_Have_Valid_Schemas()
    {
        foreach (var tool in DotNetToolDefinitions.All)
        {
            Assert.False(string.IsNullOrEmpty(tool.Name));
            Assert.False(string.IsNullOrEmpty(tool.Description));
            var doc = JsonDocument.Parse(tool.InputSchema!);
            Assert.Equal("object", doc.RootElement.GetProperty("type").GetString());
        }
    }

    [Fact]
    public void BuildProject_Supports_Configuration_Enum()
    {
        var schema = JsonDocument.Parse(DotNetToolDefinitions.BuildProject.InputSchema!).RootElement;
        var configProp = schema.GetProperty("properties").GetProperty("configuration");
        var enumValues = configProp.GetProperty("enum").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("Debug", enumValues);
        Assert.Contains("Release", enumValues);
    }

    [Fact]
    public void AddPackage_Requires_ProjectPath_And_PackageName()
    {
        var schema = JsonDocument.Parse(DotNetToolDefinitions.AddPackage.InputSchema!).RootElement;
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("projectPath", required);
        Assert.Contains("packageName", required);
    }
}

public class DotNetMcpClientTests
{
    private static DotNetMcpClient CreateClient()
        => new(NullLogger<DotNetMcpClient>.Instance);

    [Fact]
    public async Task ListToolsAsync_Returns_All_Tools()
    {
        var client = CreateClient();
        var tools = await client.ListToolsAsync();
        Assert.Equal(5, tools.Count);
    }

    [Fact]
    public async Task ExecuteToolAsync_UnknownTool_Returns_Error()
    {
        var client = CreateClient();
        var result = await client.ExecuteToolAsync(new McpToolCall { ToolName = "dotnet_unknown", Arguments = "{}" });
        Assert.True(result.IsError);
        Assert.Contains("Unknown tool", result.Content);
    }

    [Fact]
    public async Task IsHealthyAsync_Returns_True_WhenDotNetAvailable()
    {
        var client = CreateClient();
        var healthy = await client.IsHealthyAsync();
        Assert.True(healthy);
    }

    [Fact]
    public async Task ExecuteToolAsync_InvalidArguments_Returns_Error()
    {
        var client = CreateClient();
        var result = await client.ExecuteToolAsync(
            new McpToolCall { ToolName = "dotnet_build_project", Arguments = "bad-json" });
        Assert.True(result.IsError);
    }
}
