using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Core.Interfaces;

public interface IMcpGateway
{
    Task<IMcpClient> GetClientAsync(McpServerType serverType, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<McpServerType, bool>> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}
