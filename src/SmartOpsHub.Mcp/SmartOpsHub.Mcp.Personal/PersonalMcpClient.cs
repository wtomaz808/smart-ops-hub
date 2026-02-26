using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Mcp.Personal;

public sealed class PersonalMcpClient : IMcpClient
{
    private readonly IReadOnlyList<IPersonalPlugin> _plugins;

    public PersonalMcpClient(IEnumerable<IPersonalPlugin> plugins)
    {
        _plugins = plugins.ToList();
    }

    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        var tools = _plugins.SelectMany(p => p.GetTools()).ToList();
        return Task.FromResult<IReadOnlyList<McpToolDefinition>>(tools);
    }

    public Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken cancellationToken = default)
    {
        foreach (var plugin in _plugins)
        {
            var toolNames = plugin.GetTools().Select(t => t.Name);
            if (toolNames.Contains(toolCall.ToolName))
            {
                return Task.FromResult(plugin.ExecuteTool(toolCall));
            }
        }

        return Task.FromResult(new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = $"Unknown tool: {toolCall.ToolName}",
            IsError = true
        });
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
