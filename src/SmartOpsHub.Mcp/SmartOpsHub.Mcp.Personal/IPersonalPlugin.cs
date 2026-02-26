using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Mcp.Personal;

public interface IPersonalPlugin
{
    string Name { get; }
    string Description { get; }
    IReadOnlyList<McpToolDefinition> GetTools();
    McpToolResult ExecuteTool(McpToolCall toolCall);
}
