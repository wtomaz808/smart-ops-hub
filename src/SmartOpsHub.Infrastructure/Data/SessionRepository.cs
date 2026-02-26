using Microsoft.EntityFrameworkCore;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Infrastructure.Data;

public sealed class SessionRepository(SmartOpsHubDbContext dbContext, IAgentRegistry agentRegistry) : ISessionRepository
{
    public async Task<AgentSession?> GetByIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.AgentSessions
            .Include(s => s.Messages.OrderBy(m => m.Timestamp))
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ToAgentSession(entity);
    }

    public async Task<IReadOnlyList<AgentSession>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.AgentSessions
            .Include(s => s.Messages.OrderBy(m => m.Timestamp))
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.Select(ToAgentSession).ToList();
    }

    public async Task SaveAsync(AgentSession session, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.AgentSessions
            .FirstOrDefaultAsync(s => s.SessionId == session.SessionId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var entity = new SessionEntity
            {
                SessionId = session.SessionId,
                UserId = session.UserId,
                AgentType = session.AgentType.ToString(),
                AgentName = session.Agent.Name,
                SystemPrompt = session.Agent.SystemPrompt,
                Status = session.Status.ToString(),
                CreatedAt = session.CreatedAt,
                LastActivityAt = session.LastActivityAt
            };
            dbContext.AgentSessions.Add(entity);
        }
        else
        {
            existing.Status = session.Status.ToString();
            existing.LastActivityAt = session.LastActivityAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateStatusAsync(string sessionId, AgentSessionStatus status, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.AgentSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is not null)
        {
            entity.Status = status.ToString();
            entity.LastActivityAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var messages = await dbContext.ConversationLogs
            .Where(m => m.SessionId == sessionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        dbContext.ConversationLogs.RemoveRange(messages);

        var session = await dbContext.AgentSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken)
            .ConfigureAwait(false);
        if (session is not null)
            dbContext.AgentSessions.Remove(session);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private AgentSession ToAgentSession(SessionEntity entity)
    {
        var agentType = Enum.Parse<AgentType>(entity.AgentType);
        var agent = agentRegistry.GetAgent(agentType) ?? new AgentDefinition
        {
            Id = entity.AgentType.ToLowerInvariant(),
            Name = entity.AgentName,
            Type = agentType,
            Description = entity.AgentName,
            SystemPrompt = entity.SystemPrompt
        };

        var session = new AgentSession
        {
            SessionId = entity.SessionId,
            UserId = entity.UserId,
            AgentType = agentType,
            Agent = agent,
            CreatedAt = entity.CreatedAt,
            LastActivityAt = entity.LastActivityAt
        };

        session.Status = Enum.Parse<AgentSessionStatus>(entity.Status);

        foreach (var msg in entity.Messages)
        {
            session.AddMessage(new ChatMessage
            {
                Id = msg.Id.ToString(),
                Role = Enum.Parse<ChatRole>(msg.Role),
                Content = msg.MessageContent,
                Timestamp = msg.Timestamp
            });
        }

        // Reset LastActivityAt since AddMessage updates it
        session.LastActivityAt = entity.LastActivityAt;

        return session;
    }
}
