using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.Personal;
using SmartOpsHub.Mcp.Personal.Plugins;

namespace SmartOpsHub.Mcp.Tests;

public class PersonalMcpClientTests
{
    private static PersonalMcpClient CreateClient()
    {
        IPersonalPlugin[] plugins = [new FantasyFootballPlugin(), new CalendarPlugin()];
        return new PersonalMcpClient(plugins);
    }

    [Fact]
    public async Task ListToolsAsync_Returns_AllPluginTools()
    {
        var client = CreateClient();
        var tools = await client.ListToolsAsync();
        // 4 fantasy football + 3 calendar = 7
        Assert.Equal(7, tools.Count);
    }

    [Fact]
    public async Task ExecuteToolAsync_UnknownTool_Returns_Error()
    {
        var client = CreateClient();
        var result = await client.ExecuteToolAsync(new McpToolCall { ToolName = "personal_unknown", Arguments = "{}" });
        Assert.True(result.IsError);
        Assert.Contains("Unknown tool", result.Content);
    }

    [Fact]
    public async Task ExecuteToolAsync_FantasyFootballTool_Returns_Result()
    {
        var client = CreateClient();
        var result = await client.ExecuteToolAsync(new McpToolCall
        {
            ToolName = "personal_ff_get_standings",
            Arguments = """{"leagueId": "123"}"""
        });
        Assert.False(result.IsError);
        Assert.Contains("standings", result.Content);
    }

    [Fact]
    public async Task ExecuteToolAsync_CalendarTool_Returns_Result()
    {
        var client = CreateClient();
        var result = await client.ExecuteToolAsync(new McpToolCall
        {
            ToolName = "personal_cal_get_events",
            Arguments = """{"startDate": "2024-01-15"}"""
        });
        Assert.False(result.IsError);
        Assert.Contains("events", result.Content);
    }

    [Fact]
    public async Task IsHealthyAsync_Returns_True()
    {
        var client = CreateClient();
        Assert.True(await client.IsHealthyAsync());
    }
}

public class FantasyFootballPluginTests
{
    [Fact]
    public void GetTools_Returns_Four_Tools()
    {
        var plugin = new FantasyFootballPlugin();
        Assert.Equal(4, plugin.GetTools().Count);
    }

    [Fact]
    public void Name_IsFantasyFootball()
    {
        var plugin = new FantasyFootballPlugin();
        Assert.Equal("FantasyFootball", plugin.Name);
    }

    [Fact]
    public void ExecuteTool_UnknownTool_Returns_Error()
    {
        var plugin = new FantasyFootballPlugin();
        var result = plugin.ExecuteTool(new McpToolCall { ToolName = "unknown", Arguments = "{}" });
        Assert.True(result.IsError);
    }
}

public class CalendarPluginTests
{
    [Fact]
    public void GetTools_Returns_Three_Tools()
    {
        var plugin = new CalendarPlugin();
        Assert.Equal(3, plugin.GetTools().Count);
    }

    [Fact]
    public void Name_IsCalendar()
    {
        var plugin = new CalendarPlugin();
        Assert.Equal("Calendar", plugin.Name);
    }

    [Fact]
    public void ExecuteTool_CreateEvent_Returns_Result()
    {
        var plugin = new CalendarPlugin();
        var result = plugin.ExecuteTool(new McpToolCall
        {
            ToolName = "personal_cal_create_event",
            Arguments = """{"title": "Test", "startTime": "2024-01-15T09:00:00"}"""
        });
        Assert.False(result.IsError);
        Assert.Contains("created", result.Content);
    }
}
