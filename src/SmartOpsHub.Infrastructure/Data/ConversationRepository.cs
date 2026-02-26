using Microsoft.EntityFrameworkCore;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Infrastructure.Data;

public sealed class ConversationRepository(SmartOpsHubDbContext dbContext) : IConversationRepository
{
    public async Task AddMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        var session = await dbContext.AgentSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (session is null)
            return;

        var log = new ConversationLog
        {
            SessionId = sessionId,
            UserId = session.UserId,
            AgentType = session.AgentType,
            MessageContent = message.Content,
            Role = message.Role.ToString(),
            Timestamp = message.Timestamp
        };

        dbContext.ConversationLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var logs = await dbContext.ConversationLogs
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return logs.Select(log => new ChatMessage
        {
            Id = log.Id.ToString(),
            Role = Enum.Parse<ChatRole>(log.Role),
            Content = log.MessageContent,
            Timestamp = log.Timestamp
        }).ToList();
    }

    public async Task DeleteBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var messages = await dbContext.ConversationLogs
            .Where(m => m.SessionId == sessionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        dbContext.ConversationLogs.RemoveRange(messages);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
