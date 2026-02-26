using System.Text.Json;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.GitHub.Tools;

namespace SmartOpsHub.Mcp.GitHub;

public sealed class GitHubMcpClient : IMcpClient
{
    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(GitHubToolDefinitions.All);

    public Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken cancellationToken = default)
    {
        var result = toolCall.ToolName switch
        {
            "github_create_issue" => HandleCreateIssue(toolCall),
            "github_list_issues" => HandleListIssues(toolCall),
            "github_create_pull_request" => HandleCreatePullRequest(toolCall),
            "github_list_pull_requests" => HandleListPullRequests(toolCall),
            "github_get_repository" => HandleGetRepository(toolCall),
            "github_search_code" => HandleSearchCode(toolCall),
            _ => new McpToolResult { ToolCallId = toolCall.Id, Content = $"Unknown tool: {toolCall.ToolName}", IsError = true }
        };

        return Task.FromResult(result);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    private static McpToolResult HandleCreateIssue(McpToolCall toolCall)
        => new() { ToolCallId = toolCall.Id, Content = JsonSerializer.Serialize(new { id = 1, title = "Stub issue", state = "open" }) };

    private static McpToolResult HandleListIssues(McpToolCall toolCall)
        => new() { ToolCallId = toolCall.Id, Content = JsonSerializer.Serialize(new[] { new { id = 1, title = "Sample issue", state = "open" } }) };

    private static McpToolResult HandleCreatePullRequest(McpToolCall toolCall)
        => new() { ToolCallId = toolCall.Id, Content = JsonSerializer.Serialize(new { id = 1, title = "Stub PR", state = "open" }) };

    private static McpToolResult HandleListPullRequests(McpToolCall toolCall)
        => new() { ToolCallId = toolCall.Id, Content = JsonSerializer.Serialize(new[] { new { id = 1, title = "Sample PR", state = "open" } }) };

    private static McpToolResult HandleGetRepository(McpToolCall toolCall)
        => new() { ToolCallId = toolCall.Id, Content = JsonSerializer.Serialize(new { name = "stub-repo", full_name = "owner/stub-repo" }) };

    private static McpToolResult HandleSearchCode(McpToolCall toolCall)
        => new() { ToolCallId = toolCall.Id, Content = JsonSerializer.Serialize(new { total_count = 0, items = Array.Empty<object>() }) };
}
