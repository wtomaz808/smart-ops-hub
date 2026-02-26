using System.Collections.Concurrent;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Web.Services;

public sealed partial class AgentSessionManager
{
    private readonly ConcurrentDictionary<string, Dictionary<string, AgentSession>> _userSessions = new();
    private readonly HttpClient _httpClient;
    private readonly ILogger<AgentSessionManager> _logger;

    public AgentSessionManager(HttpClient httpClient, ILogger<AgentSessionManager> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AgentSession> CreateSessionAsync(string userId, AgentDefinition agent)
    {
        var session = new AgentSession
        {
            UserId = userId,
            AgentType = agent.Type,
            Agent = agent
        };

        var sessions = _userSessions.GetOrAdd(userId, _ => new Dictionary<string, AgentSession>());
        sessions[agent.Id] = session;

        LogSessionCreated(_logger, session.SessionId, userId, agent.Id);

        try
        {
            await _httpClient.PostAsJsonAsync("api/sessions", new
            {
                session.SessionId,
                UserId = userId,
                AgentId = agent.Id,
                AgentType = agent.Type.ToString()
            });
        }
        catch (Exception ex)
        {
            LogSessionRegistrationFailed(_logger, ex);
        }

        return session;
    }

    public AgentSession? GetSession(string userId, string agentId)
    {
        if (_userSessions.TryGetValue(userId, out var sessions) &&
            sessions.TryGetValue(agentId, out var session))
        {
            return session;
        }

        return null;
    }

    public IReadOnlyList<AgentSession> GetAllSessions(string userId)
    {
        if (_userSessions.TryGetValue(userId, out var sessions))
        {
            return sessions.Values.ToList();
        }

        return [];
    }

    public void RemoveSession(string userId, string agentId)
    {
        if (_userSessions.TryGetValue(userId, out var sessions))
        {
            sessions.Remove(agentId);
            LogSessionRemoved(_logger, userId, agentId);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created session {SessionId} for user {UserId} with agent {AgentId}")]
    private static partial void LogSessionCreated(ILogger logger, string sessionId, string userId, string agentId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to register session with API")]
    private static partial void LogSessionRegistrationFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Removed session for user {UserId} with agent {AgentId}")]
    private static partial void LogSessionRemoved(ILogger logger, string userId, string agentId);
}
