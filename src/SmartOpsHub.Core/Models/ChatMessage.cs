namespace SmartOpsHub.Core.Models;

public enum ChatRole
{
    User,
    Assistant,
    System,
    Tool
}

public sealed record ChatMessage
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required ChatRole Role { get; init; }
    public required string Content { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string? ToolCallId { get; init; }
    public string? ToolName { get; init; }
}
