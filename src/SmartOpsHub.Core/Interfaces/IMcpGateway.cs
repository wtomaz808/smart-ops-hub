using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Core.Interfaces;

public interface IMcpGateway
{
    Task<IMcpClient> GetClientAsync(AgentType agentType, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<AgentType, bool>> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}
