using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Api.Orchestration;

/// <summary>
/// Stub gateway for local development. Replace with HTTP-based gateway
/// when the MCP Gateway container is deployed.
/// </summary>
internal sealed class StubMcpGateway : IMcpGateway
{
    public Task<IMcpClient> GetClientAsync(AgentType agentType, CancellationToken cancellationToken = default)
        => Task.FromResult<IMcpClient>(new StubMcpClient());

    public Task<IReadOnlyDictionary<AgentType, bool>> GetHealthStatusAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<AgentType, bool>>(
            Enum.GetValues<AgentType>().ToDictionary(t => t, _ => false));
}

internal sealed class StubMcpClient : IMcpClient
{
    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<McpToolDefinition>>([]);

    public Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken cancellationToken = default)
        => Task.FromResult(new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = "MCP Gateway is not configured. Deploy the MCP Gateway container."
        });

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
