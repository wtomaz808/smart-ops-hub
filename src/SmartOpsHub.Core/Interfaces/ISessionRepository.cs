using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Core.Interfaces;

public interface ISessionRepository
{
    Task<AgentSession?> GetByIdAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgentSession>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task SaveAsync(AgentSession session, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string sessionId, AgentSessionStatus status, CancellationToken cancellationToken = default);
    Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default);
}
