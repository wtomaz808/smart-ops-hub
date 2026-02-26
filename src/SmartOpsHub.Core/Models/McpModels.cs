namespace SmartOpsHub.Core.Models;

public sealed record McpToolDefinition
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string? InputSchema { get; init; }
}

public sealed record McpToolCall
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string ToolName { get; init; }
    public required string Arguments { get; init; }
}

public sealed record McpToolResult
{
    public required string ToolCallId { get; init; }
    public required string Content { get; init; }
    public bool IsError { get; init; }
}
