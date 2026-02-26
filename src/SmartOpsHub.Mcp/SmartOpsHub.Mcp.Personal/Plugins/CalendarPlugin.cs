using System.Text.Json;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Mcp.Personal.Plugins;

public sealed class CalendarPlugin : IPersonalPlugin
{
    public string Name => "Calendar";
    public string Description => "Calendar management tools for events and reminders";

    public IReadOnlyList<McpToolDefinition> GetTools() =>
    [
        new() { Name = "personal_cal_get_events", Description = "Gets calendar events for a date range", InputSchema = """{"type":"object","properties":{"startDate":{"type":"string","format":"date"},"endDate":{"type":"string","format":"date"}},"required":["startDate"]}""" },
        new() { Name = "personal_cal_create_event", Description = "Creates a new calendar event", InputSchema = """{"type":"object","properties":{"title":{"type":"string"},"startTime":{"type":"string","format":"date-time"},"endTime":{"type":"string","format":"date-time"},"description":{"type":"string"}},"required":["title","startTime"]}""" },
        new() { Name = "personal_cal_get_reminders", Description = "Gets upcoming reminders", InputSchema = """{"type":"object","properties":{"count":{"type":"integer"}},"required":[]}""" }
    ];

    public McpToolResult ExecuteTool(McpToolCall toolCall) => toolCall.ToolName switch
    {
        "personal_cal_get_events" => Stub(toolCall, new { events = new[] { new { title = "Team Standup", start = "2024-01-15T09:00:00", end = "2024-01-15T09:30:00" } } }),
        "personal_cal_create_event" => Stub(toolCall, new { id = "evt-1", created = true, title = "New Event" }),
        "personal_cal_get_reminders" => Stub(toolCall, new { reminders = new[] { new { title = "Review PR", dueAt = "2024-01-15T14:00:00" } } }),
        _ => new McpToolResult { ToolCallId = toolCall.Id, Content = $"Unknown tool: {toolCall.ToolName}", IsError = true }
    };

    private static McpToolResult Stub(McpToolCall toolCall, object data)
        => new() { ToolCallId = toolCall.Id, Content = JsonSerializer.Serialize(data) };
}
