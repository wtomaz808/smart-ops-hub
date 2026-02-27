using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Web.Services;

public sealed partial class AgentChatService : IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AgentChatService> _logger;
    private readonly Dictionary<string, HubConnection> _connections = new();

    public AgentChatService(IConfiguration configuration, ILogger<AgentChatService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ConnectAsync(string agentId, string sessionId)
    {
        if (_connections.ContainsKey(agentId))
            return;

        var apiBaseUrl = _configuration.GetValue("ApiBaseUrl", "http://localhost:5100")!.TrimEnd('/');
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

        try
        {
            await connection.StartAsync();
            LogConnected(_logger, agentId);
        }
        catch (Exception ex)
        {
            LogConnectionFailed(_logger, agentId, ex);
        }
    }

    public async Task SendMessageAsync(string agentId, string message)
    {
        if (_connections.TryGetValue(agentId, out var connection) &&
            connection.State == HubConnectionState.Connected)
        {
            await connection.SendAsync("SendMessage", agentId, message);
        }
    }

    public IDisposable OnMessageReceived(string agentId, Action<ChatMessage> handler)
    {
        if (_connections.TryGetValue(agentId, out var connection))
        {
            return connection.On("ReceiveMessage", handler);
        }

        return new NoOpDisposable();
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

    public IDisposable OnStatusChanged(string agentId, Action<AgentSessionStatus> handler)
    {
        if (_connections.TryGetValue(agentId, out var connection))
        {
            return connection.On("StatusChanged", handler);
        }

        return new NoOpDisposable();
    }

    public async Task DisconnectAsync(string agentId)
    {
        if (_connections.Remove(agentId, out var connection))
        {
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
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Reconnecting to agent hub {AgentId}")]
    private static partial void LogReconnecting(ILogger logger, string agentId, Exception? exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reconnected to agent hub {AgentId} with connection {ConnectionId}")]
    private static partial void LogReconnected(ILogger logger, string agentId, string? connectionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection closed for agent hub {AgentId}")]
    private static partial void LogConnectionClosed(ILogger logger, string agentId, Exception? exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connected to agent hub {AgentId}")]
    private static partial void LogConnected(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to connect to agent hub {AgentId}")]
    private static partial void LogConnectionFailed(ILogger logger, string agentId, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Disconnected from agent hub {AgentId}")]
    private static partial void LogDisconnected(ILogger logger, string agentId);

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
