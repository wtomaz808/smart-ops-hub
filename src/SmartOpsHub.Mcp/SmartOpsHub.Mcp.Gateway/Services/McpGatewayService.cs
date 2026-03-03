using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Mcp.Gateway.Services;

public sealed class McpGatewayService : IMcpGateway
{
    private readonly Dictionary<McpServerType, IMcpClient> _clients;

    public McpGatewayService(IEnumerable<KeyValuePair<McpServerType, IMcpClient>> clients)
    {
        _clients = clients.ToDictionary(c => c.Key, c => c.Value);
    }

    public Task<IMcpClient> GetClientAsync(McpServerType serverType, CancellationToken cancellationToken = default)
    {
        if (_clients.TryGetValue(serverType, out var client))
        {
            return Task.FromResult(client);
        }

        throw new InvalidOperationException($"No MCP client registered for server type: {serverType}");
    }

    public async Task<IReadOnlyDictionary<McpServerType, bool>> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<McpServerType, bool>();

        foreach (var (serverType, client) in _clients)
        {
            try
            {
                results[serverType] = await client.IsHealthyAsync(cancellationToken);
            }
            catch
            {
                results[serverType] = false;
            }
        }

        return results;
    }
}
