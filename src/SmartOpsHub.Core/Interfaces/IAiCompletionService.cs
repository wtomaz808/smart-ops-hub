using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Core.Interfaces;

public interface IAiCompletionService
{
    Task<string> GetCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<McpToolDefinition>? availableTools = null,
        string? deploymentName = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<McpToolDefinition>? availableTools = null,
        string? deploymentName = null,
        CancellationToken cancellationToken = default);
}
