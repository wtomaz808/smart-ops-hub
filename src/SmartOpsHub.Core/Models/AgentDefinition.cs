namespace SmartOpsHub.Core.Models;

public sealed record AgentDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required AgentType Type { get; init; }
    public required string SystemPrompt { get; init; }
    public string? AvatarUrl { get; init; }
    public string McpServerEndpoint { get; init; } = string.Empty;
    public bool IsEnabled { get; init; } = true;
}
