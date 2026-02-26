using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Core.Interfaces;

public interface IAiCompletionService
{
    Task<string> GetCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<McpToolDefinition>? availableTools = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<McpToolDefinition>? availableTools = null,
        CancellationToken cancellationToken = default);
}
