using System.Collections.Concurrent;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Api.Orchestration;

public sealed partial class AgentOrchestrator(
    IAgentRegistry agentRegistry,
    IAiCompletionService aiCompletionService,
    IMcpGateway mcpGateway,
    ILogger<AgentOrchestrator> logger) : IAgentOrchestrator
{
    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();

    public Task<AgentSession> CreateSessionAsync(string userId, AgentType agentType, CancellationToken cancellationToken = default)
    {
        var agentDefinition = agentRegistry.GetAgent(agentType)
            ?? throw new ArgumentException($"No agent found for type {agentType}.", nameof(agentType));

        var session = new AgentSession
        {
            UserId = userId,
            AgentType = agentType,
            Agent = agentDefinition
        };

        session.AddMessage(new ChatMessage
        {
            Role = ChatRole.System,
            Content = agentDefinition.SystemPrompt
        });

        if (!_sessions.TryAdd(session.SessionId, session))
        {
            throw new InvalidOperationException("Failed to create session. Please try again.");
        }

        LogSessionCreated(logger, session.SessionId, userId, agentType);

        return Task.FromResult(session);
    }

    public async Task<ChatMessage> ProcessMessageAsync(string sessionId, string userMessage, CancellationToken cancellationToken = default)
    {
        var session = GetRequiredSession(sessionId);
        session.Status = AgentSessionStatus.Thinking;

        session.AddMessage(new ChatMessage
        {
            Role = ChatRole.User,
            Content = userMessage
        });

        try
        {
            var tools = await GetToolsForAgent(session.AgentType, cancellationToken);

            var responseText = await aiCompletionService.GetCompletionAsync(
                session.ConversationHistory,
                tools,
                cancellationToken);

            var assistantMessage = new ChatMessage
            {
                Role = ChatRole.Assistant,
                Content = responseText
            };

            session.AddMessage(assistantMessage);
            session.Status = AgentSessionStatus.Idle;

            LogMessageProcessed(logger, sessionId, session.ConversationHistory.Count);

            return assistantMessage;
        }
        catch (Exception ex)
        {
            session.Status = AgentSessionStatus.Error;
            LogProcessMessageError(logger, ex, sessionId);
            throw;
        }
    }

    public Task<AgentSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task EndSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryRemove(sessionId, out _))
        {
            LogSessionEnded(logger, sessionId);
        }

        return Task.CompletedTask;
    }

    private AgentSession GetRequiredSession(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var session)
            ? session
            : throw new KeyNotFoundException($"Session {sessionId} not found.");
    }

    private async Task<IReadOnlyList<McpToolDefinition>?> GetToolsForAgent(AgentType agentType, CancellationToken cancellationToken)
    {
        try
        {
            var client = await mcpGateway.GetClientAsync(agentType, cancellationToken);
            return await client.ListToolsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            LogToolRetrievalFailed(logger, ex, agentType);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created session {SessionId} for user {UserId} with agent {AgentType}")]
    private static partial void LogSessionCreated(ILogger logger, string sessionId, string userId, AgentType agentType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processed message for session {SessionId}, history length: {HistoryLength}")]
    private static partial void LogMessageProcessed(ILogger logger, string sessionId, int historyLength);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing message for session {SessionId}")]
    private static partial void LogProcessMessageError(ILogger logger, Exception ex, string sessionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Ended session {SessionId}")]
    private static partial void LogSessionEnded(ILogger logger, string sessionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to retrieve tools for agent {AgentType}, proceeding without tools")]
    private static partial void LogToolRetrievalFailed(ILogger logger, Exception ex, AgentType agentType);
}
