using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Core.Interfaces;

public interface IConversationRepository
{
    Task AddMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default);
    Task DeleteBySessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
