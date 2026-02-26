namespace SmartOpsHub.Core.Models;

public sealed record UserProfile
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Email { get; init; }
    public List<string> Roles { get; init; } = [];
    public List<AgentType> AssignedAgents { get; init; } = [];
}
