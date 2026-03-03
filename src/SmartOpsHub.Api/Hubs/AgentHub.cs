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

    public async Task SendMessage(string sessionId, string message, List<FileAttachmentDto>? attachments = null)
    {
        LogProcessingMessage(logger, sessionId);

        await SendStatusUpdate(sessionId, AgentSessionStatus.Thinking);

        // Build enriched message with file contents
        var enrichedMessage = message;
        if (attachments is { Count: > 0 })
        {
            var fileContext = new System.Text.StringBuilder();
            fileContext.AppendLine("\n\n---\nAttached files:");
            foreach (var file in attachments)
            {
                fileContext.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"\n### {file.FileName} ({file.ContentType}, {file.SizeBytes} bytes)");
                if (!string.IsNullOrEmpty(file.TextContent))
                {
                    fileContext.AppendLine("```");
                    fileContext.AppendLine(file.TextContent.Length > 50_000
                        ? string.Concat(file.TextContent.AsSpan(0, 50_000), "\n... (truncated)")
                        : file.TextContent);
                    fileContext.AppendLine("```");
                }
                else
                {
                    fileContext.AppendLine("(binary file — content not shown)");
                }
            }
            enrichedMessage = message + fileContext;
        }

        try
        {
            await foreach (var token in orchestrator.StreamMessageAsync(sessionId, enrichedMessage, Context.ConnectionAborted))
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

public sealed record FileAttachmentDto
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string? TextContent { get; init; }
    public string? Base64Data { get; init; }
}
