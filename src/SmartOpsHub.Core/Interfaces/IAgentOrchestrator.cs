using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Core.Interfaces;

public interface IAgentOrchestrator
{
    Task<AgentSession> CreateSessionAsync(string userId, AgentType agentType, CancellationToken cancellationToken = default);
    Task<ChatMessage> ProcessMessageAsync(string sessionId, string userMessage, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamMessageAsync(
        string sessionId,
        string userMessage,
        CancellationToken cancellationToken = default);

    Task<AgentSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task EndSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
