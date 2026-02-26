namespace SmartOpsHub.Infrastructure.Data;

public sealed class ConversationLog
{
    public Guid Id { get; set; }
    public required string SessionId { get; set; }
    public required string UserId { get; set; }
    public required string AgentType { get; set; }
    public required string MessageContent { get; set; }
    public required string Role { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public SessionEntity? Session { get; set; }
}
