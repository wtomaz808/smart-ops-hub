using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Web.Services;

public sealed partial class AgentChatService : IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AgentChatService> _logger;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, HubConnection> _connections = new();
    private readonly Dictionary<string, string> _sessionIds = new();

    public AgentChatService(IConfiguration configuration, ILogger<AgentChatService> logger, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("AgentApi");
    }

    public async Task<bool> ConnectAsync(string agentId, AgentCategory category)
    {
        if (_connections.ContainsKey(agentId))
            return true;

        var apiBaseUrl = _configuration.GetValue("ApiBaseUrl", "http://localhost:5100")!.TrimEnd('/');

        // 1. Create a session on the API
        string? sessionId = null;
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/sessions", new
            {
                UserId = "web-user",
                AgentCategory = (int)category
            });

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                sessionId = json.GetProperty("sessionId").GetString();
                LogSessionCreated(_logger, agentId, sessionId!);
            }
            else
            {
                LogSessionCreateFailed(_logger, agentId, response.StatusCode.ToString());
                return false;
            }
        }
        catch (Exception ex)
        {
            LogSessionCreateFailed(_logger, agentId, ex.Message);
            return false;
        }

        // 2. Connect to SignalR hub
        var hubUrl = $"{apiBaseUrl}/hubs/agent";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)])
            .Build();

        connection.Reconnecting += error =>
        {
            LogReconnecting(_logger, agentId, error);
            return Task.CompletedTask;
        };

        connection.Reconnected += connectionId =>
        {
            LogReconnected(_logger, agentId, connectionId);
            return Task.CompletedTask;
        };

        connection.Closed += error =>
        {
            LogConnectionClosed(_logger, agentId, error);
            return Task.CompletedTask;
        };

        _connections[agentId] = connection;
        _sessionIds[agentId] = sessionId!;

        try
        {
            await connection.StartAsync();
            LogConnected(_logger, agentId);

            // 3. Join the session group so we receive responses
            await connection.InvokeAsync("JoinSession", sessionId);
            LogJoinedSession(_logger, agentId, sessionId!);
            return true;
        }
        catch (Exception ex)
        {
            LogConnectionFailed(_logger, agentId, ex);
            _connections.Remove(agentId);
            _sessionIds.Remove(agentId);
            return false;
        }
    }

    public async Task SendMessageAsync(string agentId, string message, List<FileAttachment>? attachments = null, string? model = null)
    {
        if (!_connections.TryGetValue(agentId, out var connection) ||
            connection.State != HubConnectionState.Connected ||
            !_sessionIds.TryGetValue(agentId, out var sessionId))
        {
            return;
        }

        var fileData = attachments?.Select(a => new
        {
            a.FileName,
            a.ContentType,
            a.SizeBytes,
            a.TextContent,
            a.Base64Data
        }).ToList();

        await connection.SendAsync("SendMessage", sessionId, message, fileData, model);
    }

    public IDisposable OnStreamToken(string agentId, Action<string> handler)
    {
        if (_connections.TryGetValue(agentId, out var connection))
        {
            return connection.On("ReceiveStreamToken", handler);
        }
        return new NoOpDisposable();
    }

    public IDisposable OnStreamComplete(string agentId, Action handler)
    {
        if (_connections.TryGetValue(agentId, out var connection))
        {
            return connection.On("StreamComplete", handler);
        }
        return new NoOpDisposable();
    }

    public IDisposable OnStatusChanged(string agentId, Action<string> handler)
    {
        if (_connections.TryGetValue(agentId, out var connection))
        {
            return connection.On("StatusUpdate", handler);
        }
        return new NoOpDisposable();
    }

    public IDisposable OnError(string agentId, Action<string> handler)
    {
        if (_connections.TryGetValue(agentId, out var connection))
        {
            return connection.On("ReceiveError", handler);
        }
        return new NoOpDisposable();
    }

    public async Task DisconnectAsync(string agentId)
    {
        if (_connections.Remove(agentId, out var connection))
        {
            if (_sessionIds.TryGetValue(agentId, out var sessionId))
            {
                try { await connection.InvokeAsync("LeaveSession", sessionId); } catch { /* best effort */ }
            }
            _sessionIds.Remove(agentId);
            await connection.DisposeAsync();
            LogDisconnected(_logger, agentId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var connection in _connections.Values)
        {
            await connection.DisposeAsync();
        }
        _connections.Clear();
        _sessionIds.Clear();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created API session for agent {AgentId}: {SessionId}")]
    private static partial void LogSessionCreated(ILogger logger, string agentId, string sessionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to create session for agent {AgentId}: {Error}")]
    private static partial void LogSessionCreateFailed(ILogger logger, string agentId, string error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Reconnecting to agent hub {AgentId}")]
    private static partial void LogReconnecting(ILogger logger, string agentId, Exception? exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reconnected to agent hub {AgentId} with connection {ConnectionId}")]
    private static partial void LogReconnected(ILogger logger, string agentId, string? connectionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection closed for agent hub {AgentId}")]
    private static partial void LogConnectionClosed(ILogger logger, string agentId, Exception? exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connected to agent hub {AgentId}")]
    private static partial void LogConnected(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Joined session for agent {AgentId}: {SessionId}")]
    private static partial void LogJoinedSession(ILogger logger, string agentId, string sessionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to connect to agent hub {AgentId}")]
    private static partial void LogConnectionFailed(ILogger logger, string agentId, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Disconnected from agent hub {AgentId}")]
    private static partial void LogDisconnected(ILogger logger, string agentId);

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
