namespace SmartOpsHub.Core.Models;

public enum AgentSessionStatus
{
    Idle,
    Thinking,
    Working,
    Error
}

public sealed class AgentSession
{
    public string SessionId { get; init; } = Guid.NewGuid().ToString();
    public required string UserId { get; init; }
    public required AgentType AgentType { get; init; }
    public required AgentDefinition Agent { get; init; }
    public AgentSessionStatus Status { get; set; } = AgentSessionStatus.Idle;
    public List<ChatMessage> ConversationHistory { get; } = [];
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;

    public void AddMessage(ChatMessage message)
    {
        ConversationHistory.Add(message);
        LastActivityAt = DateTimeOffset.UtcNow;
    }
}
