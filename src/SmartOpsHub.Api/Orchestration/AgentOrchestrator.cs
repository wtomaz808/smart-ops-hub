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
    McpToolExecutor mcpToolExecutor,
    ISessionRepository sessionRepository,
    IConversationRepository conversationRepository,
    ILogger<AgentOrchestrator> logger) : IAgentOrchestrator
{
    private const int MaxToolRounds = 5;
    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new();

    public async Task<AgentSession> CreateSessionAsync(string userId, AgentCategory agentCategory, CancellationToken cancellationToken = default)
    {
        var agentDefinition = agentRegistry.GetAgent(agentCategory)
            ?? throw new ArgumentException($"No agent found for category {agentCategory}.", nameof(agentCategory));

        var session = new AgentSession
        {
            UserId = userId,
            AgentCategory = agentCategory,
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

        LogSessionCreated(logger, session.SessionId, userId, agentCategory);

        return session;
    }

    public async Task<AgentSession> FindOrCreateSessionAsync(string userId, AgentCategory agentCategory, CancellationToken cancellationToken = default)
    {
        // Check in-memory cache first
        var cached = _sessions.Values.FirstOrDefault(s => s.UserId == userId && s.AgentCategory == agentCategory);
        if (cached is not null)
        {
            LogSessionResumed(logger, cached.SessionId, userId, agentCategory);
            return cached;
        }

        // Check database for existing session
        var existing = await sessionRepository.GetByUserAndCategoryAsync(userId, agentCategory, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            _sessions.TryAdd(existing.SessionId, existing);
            LogSessionResumed(logger, existing.SessionId, userId, agentCategory);
            return existing;
        }

        // No existing session — create a new one
        return await CreateSessionAsync(userId, agentCategory, cancellationToken).ConfigureAwait(false);
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
            var (tools, _) = await GetToolsWithMappingForAgent(session.AgentCategory, cancellationToken).ConfigureAwait(false);

            var responseText = await aiCompletionService.GetCompletionAsync(
                session.ConversationHistory,
                tools,
                null,
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
        string? deploymentName = null,
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

        var (tools, toolServerMap) = await GetToolsWithMappingForAgent(session.AgentCategory, cancellationToken).ConfigureAwait(false);

        var fullResponse = new StringBuilder();

        for (var round = 0; round < MaxToolRounds; round++)
        {
            var toolCalls = new List<AiToolCallRequest>();

            await foreach (var evt in aiCompletionService.StreamWithToolDetectionAsync(
                session.ConversationHistory, tools, deploymentName, cancellationToken).ConfigureAwait(false))
            {
                if (evt is TextTokenEvent textToken)
                {
                    fullResponse.Append(textToken.Text);
                    yield return textToken.Text;
                }
                else if (evt is ToolCallsCompleteEvent toolCallEvt)
                {
                    toolCalls.AddRange(toolCallEvt.ToolCalls);
                }
            }

            if (toolCalls.Count == 0)
            {
                break;
            }

            // Add assistant message with tool calls to conversation
            var assistantToolMsg = new ChatMessage
            {
                Role = ChatRole.Assistant,
                Content = fullResponse.ToString(),
                ToolCalls = toolCalls
            };
            session.AddMessage(assistantToolMsg);
            fullResponse.Clear();

            // Execute each tool call and add results
            foreach (var toolCall in toolCalls)
            {
                var serverType = toolServerMap.GetValueOrDefault(toolCall.FunctionName);
                var mcpToolCall = new McpToolCall
                {
                    Id = toolCall.Id,
                    ToolName = toolCall.FunctionName,
                    Arguments = toolCall.Arguments
                };

                LogToolExecution(logger, toolCall.FunctionName, serverType);
                var result = await mcpToolExecutor.ExecuteAsync(serverType, mcpToolCall, cancellationToken).ConfigureAwait(false);

                var toolResultMsg = new ChatMessage
                {
                    Role = ChatRole.Tool,
                    Content = result.Content,
                    ToolCallId = toolCall.Id,
                    ToolName = toolCall.FunctionName
                };
                session.AddMessage(toolResultMsg);
            }
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

    private async Task<(IReadOnlyList<McpToolDefinition>? Tools, Dictionary<string, McpServerType> ToolServerMap)> GetToolsWithMappingForAgent(
        AgentCategory agentCategory, CancellationToken cancellationToken)
    {
        var agent = agentRegistry.GetAgent(agentCategory);
        if (agent is null || agent.McpServers.Length == 0)
            return (null, new Dictionary<string, McpServerType>());

        var allTools = new List<McpToolDefinition>();
        var toolServerMap = new Dictionary<string, McpServerType>(StringComparer.OrdinalIgnoreCase);

        foreach (var serverType in agent.McpServers)
        {
            try
            {
                var client = await mcpGateway.GetClientAsync(serverType, cancellationToken);
                var tools = await client.ListToolsAsync(cancellationToken);
                allTools.AddRange(tools);

                foreach (var tool in tools)
                {
                    toolServerMap.TryAdd(tool.Name, serverType);
                }
            }
            catch (Exception ex)
            {
                LogToolRetrievalFailed(logger, ex, serverType);
            }
        }

        return (allTools.Count > 0 ? allTools : null, toolServerMap);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created session {SessionId} for user {UserId} with agent {AgentCategory}")]
    private static partial void LogSessionCreated(ILogger logger, string sessionId, string userId, AgentCategory agentCategory);

    [LoggerMessage(Level = LogLevel.Information, Message = "Resumed session {SessionId} for user {UserId} with agent {AgentCategory}")]
    private static partial void LogSessionResumed(ILogger logger, string sessionId, string userId, AgentCategory agentCategory);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processed message for session {SessionId}, history length: {HistoryLength}")]
    private static partial void LogMessageProcessed(ILogger logger, string sessionId, int historyLength);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing message for session {SessionId}")]
    private static partial void LogProcessMessageError(ILogger logger, Exception ex, string sessionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Ended session {SessionId}")]
    private static partial void LogSessionEnded(ILogger logger, string sessionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to retrieve tools for MCP server {ServerType}, proceeding without tools")]
    private static partial void LogToolRetrievalFailed(ILogger logger, Exception ex, McpServerType serverType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing tool {ToolName} via MCP server {ServerType}")]
    private static partial void LogToolExecution(ILogger logger, string toolName, McpServerType serverType);
}
