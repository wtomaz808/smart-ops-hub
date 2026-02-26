namespace SmartOpsHub.Infrastructure.Data;

public sealed class SessionEntity
{
    public required string SessionId { get; set; }
    public required string UserId { get; set; }
    public required string AgentType { get; set; }
    public required string AgentName { get; set; }
    public required string SystemPrompt { get; set; }
    public string Status { get; set; } = "Idle";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;

    public List<ConversationLog> Messages { get; set; } = [];
}
