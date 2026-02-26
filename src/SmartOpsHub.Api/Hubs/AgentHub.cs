using Microsoft.AspNetCore.SignalR;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Api.Hubs;

public sealed partial class AgentHub(
    IAgentOrchestrator orchestrator,
    ILogger<AgentHub> logger) : Hub
{
    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        LogConnectionJoined(logger, Context.ConnectionId, sessionId);
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
        LogConnectionLeft(logger, Context.ConnectionId, sessionId);
    }

    public async Task SendMessage(string sessionId, string message)
    {
        LogProcessingMessage(logger, sessionId);

        await SendStatusUpdate(sessionId, AgentSessionStatus.Thinking);

        try
        {
            await foreach (var token in orchestrator.StreamMessageAsync(sessionId, message, Context.ConnectionAborted))
            {
                await Clients.Group(sessionId).SendAsync("ReceiveStreamToken", token, Context.ConnectionAborted);
            }

            await Clients.Group(sessionId).SendAsync("StreamComplete", Context.ConnectionAborted);
            await SendStatusUpdate(sessionId, AgentSessionStatus.Idle);
        }
        catch (Exception ex)
        {
            LogMessageError(logger, ex, sessionId);
            await SendStatusUpdate(sessionId, AgentSessionStatus.Error);
            throw;
        }
    }

    public async Task SendStatusUpdate(string sessionId, AgentSessionStatus status)
    {
        await Clients.Group(sessionId).SendAsync("StatusUpdate", status.ToString(), Context.ConnectionAborted);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Connection {ConnectionId} joined session {SessionId}")]
    private static partial void LogConnectionJoined(ILogger logger, string connectionId, string sessionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connection {ConnectionId} left session {SessionId}")]
    private static partial void LogConnectionLeft(ILogger logger, string connectionId, string sessionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Processing message for session {SessionId}")]
    private static partial void LogProcessingMessage(ILogger logger, string sessionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error processing message for session {SessionId}")]
    private static partial void LogMessageError(ILogger logger, Exception ex, string sessionId);
}
