using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Api.Orchestration;

public sealed partial class McpToolExecutor(
    IMcpGateway mcpGateway,
    ILogger<McpToolExecutor> logger)
{
    public async Task<McpToolResult> ExecuteAsync(AgentType agentType, McpToolCall toolCall, CancellationToken cancellationToken = default)
    {
        LogExecutingTool(logger, toolCall.ToolName, agentType);

        try
        {
            var client = await mcpGateway.GetClientAsync(agentType, cancellationToken);
            var result = await client.ExecuteToolAsync(toolCall, cancellationToken);

            LogToolExecuted(logger, toolCall.ToolName, result.IsError);

            return result;
        }
        catch (Exception ex)
        {
            LogToolExecutionFailed(logger, ex, toolCall.ToolName, agentType);

            return new McpToolResult
            {
                ToolCallId = toolCall.Id,
                Content = $"Tool execution failed: {ex.Message}",
                IsError = true
            };
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing tool {ToolName} for agent {AgentType}")]
    private static partial void LogExecutingTool(ILogger logger, string toolName, AgentType agentType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tool {ToolName} executed successfully, isError: {IsError}")]
    private static partial void LogToolExecuted(ILogger logger, string toolName, bool isError);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to execute tool {ToolName} for agent {AgentType}")]
    private static partial void LogToolExecutionFailed(ILogger logger, Exception ex, string toolName, AgentType agentType);
}
