using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Mcp.Gateway.Services;

public sealed class McpGatewayService : IMcpGateway
{
    private readonly Dictionary<AgentType, IMcpClient> _clients;

    public McpGatewayService(IEnumerable<KeyValuePair<AgentType, IMcpClient>> clients)
    {
        _clients = clients.ToDictionary(c => c.Key, c => c.Value);
    }

    public Task<IMcpClient> GetClientAsync(AgentType agentType, CancellationToken cancellationToken = default)
    {
        if (_clients.TryGetValue(agentType, out var client))
        {
            return Task.FromResult(client);
        }

        throw new InvalidOperationException($"No MCP client registered for agent type: {agentType}");
    }

    public async Task<IReadOnlyDictionary<AgentType, bool>> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<AgentType, bool>();

        foreach (var (agentType, client) in _clients)
        {
            try
            {
                results[agentType] = await client.IsHealthyAsync(cancellationToken);
            }
            catch
            {
                results[agentType] = false;
            }
        }

        return results;
    }
}
