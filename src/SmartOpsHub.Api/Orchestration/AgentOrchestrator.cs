using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Api.Orchestration;

public sealed partial class AgentOrchestrator(
    IAgentRegistry agentRegistry,
    IAiCompletionService aiCompletionService,
    IMcpGateway mcpGateway,
    ISessionRepository sessionRepository,
    IConversationRepository conversationRepository,
    ILogger<AgentOrchestrator> logger) : IAgentOrchestrator
{
    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();

    public async Task<AgentSession> CreateSessionAsync(string userId, AgentType agentType, CancellationToken cancellationToken = default)
    {
        var agentDefinition = agentRegistry.GetAgent(agentType)
            ?? throw new ArgumentException($"No agent found for type {agentType}.", nameof(agentType));

        var session = new AgentSession
        {
            UserId = userId,
            AgentType = agentType,
            Agent = agentDefinition
        };

        var systemMessage = new ChatMessage
        {
            Role = ChatRole.System,
            Content = agentDefinition.SystemPrompt
        };
        session.AddMessage(systemMessage);

        if (!_sessions.TryAdd(session.SessionId, session))
        {
            throw new InvalidOperationException("Failed to create session. Please try again.");
        }

        await sessionRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);
        await conversationRepository.AddMessageAsync(session.SessionId, systemMessage, cancellationToken).ConfigureAwait(false);

        LogSessionCreated(logger, session.SessionId, userId, agentType);

        return session;
    }

    public async Task<ChatMessage> ProcessMessageAsync(string sessionId, string userMessage, CancellationToken cancellationToken = default)
    {
        var session = await GetRequiredSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        session.Status = AgentSessionStatus.Thinking;

        var userMsg = new ChatMessage
        {
            Role = ChatRole.User,
            Content = userMessage
        };
        session.AddMessage(userMsg);
        await conversationRepository.AddMessageAsync(sessionId, userMsg, cancellationToken).ConfigureAwait(false);

        try
        {
            var tools = await GetToolsForAgent(session.AgentType, cancellationToken).ConfigureAwait(false);

            var responseText = await aiCompletionService.GetCompletionAsync(
                session.ConversationHistory,
                tools,
                cancellationToken).ConfigureAwait(false);

            var assistantMessage = new ChatMessage
            {
                Role = ChatRole.Assistant,
                Content = responseText
            };

            session.AddMessage(assistantMessage);
            session.Status = AgentSessionStatus.Idle;

            await conversationRepository.AddMessageAsync(sessionId, assistantMessage, cancellationToken).ConfigureAwait(false);
            await sessionRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);

            LogMessageProcessed(logger, sessionId, session.ConversationHistory.Count);

            return assistantMessage;
        }
        catch (Exception ex)
        {
            session.Status = AgentSessionStatus.Error;
            await sessionRepository.UpdateStatusAsync(sessionId, AgentSessionStatus.Error, cancellationToken).ConfigureAwait(false);
            LogProcessMessageError(logger, ex, sessionId);
            throw;
        }
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(
        string sessionId,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var session = await GetRequiredSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        session.Status = AgentSessionStatus.Thinking;

        var userMsg = new ChatMessage
        {
            Role = ChatRole.User,
            Content = userMessage
        };
        session.AddMessage(userMsg);
        await conversationRepository.AddMessageAsync(sessionId, userMsg, cancellationToken).ConfigureAwait(false);

        var tools = await GetToolsForAgent(session.AgentType, cancellationToken).ConfigureAwait(false);

        var fullResponse = new StringBuilder();
        await foreach (var token in aiCompletionService.StreamCompletionAsync(
            session.ConversationHistory, tools, cancellationToken).ConfigureAwait(false))
        {
            fullResponse.Append(token);
            yield return token;
        }

        var assistantMessage = new ChatMessage
        {
            Role = ChatRole.Assistant,
            Content = fullResponse.ToString()
        };

        session.AddMessage(assistantMessage);
        session.Status = AgentSessionStatus.Idle;

        await conversationRepository.AddMessageAsync(sessionId, assistantMessage, cancellationToken).ConfigureAwait(false);
        await sessionRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);

        LogMessageProcessed(logger, sessionId, session.ConversationHistory.Count);
    }

    public async Task<AgentSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryGetValue(sessionId, out var cached))
            return cached;

        // Fall back to database
        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is not null)
            _sessions.TryAdd(sessionId, session);

        return session;
    }

    public async Task EndSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(sessionId, out _);
        await sessionRepository.DeleteAsync(sessionId, cancellationToken).ConfigureAwait(false);

        LogSessionEnded(logger, sessionId);
    }

    private async Task<AgentSession> GetRequiredSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        return session ?? throw new KeyNotFoundException($"Session {sessionId} not found.");
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
