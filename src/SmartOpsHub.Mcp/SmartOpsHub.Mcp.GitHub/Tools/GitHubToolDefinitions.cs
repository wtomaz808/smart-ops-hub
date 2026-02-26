using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Mcp.GitHub.Tools;

public static class GitHubToolDefinitions
{
    public static readonly McpToolDefinition CreateIssue = new()
    {
        Name = "github_create_issue",
        Description = "Creates a new issue in a GitHub repository",
        InputSchema = """{"type":"object","properties":{"owner":{"type":"string"},"repo":{"type":"string"},"title":{"type":"string"},"body":{"type":"string"},"labels":{"type":"array","items":{"type":"string"}}},"required":["owner","repo","title"]}"""
    };

    public static readonly McpToolDefinition ListIssues = new()
    {
        Name = "github_list_issues",
        Description = "Lists issues in a GitHub repository",
        InputSchema = """{"type":"object","properties":{"owner":{"type":"string"},"repo":{"type":"string"},"state":{"type":"string","enum":["open","closed","all"]}},"required":["owner","repo"]}"""
    };

    public static readonly McpToolDefinition CreatePullRequest = new()
    {
        Name = "github_create_pull_request",
        Description = "Creates a new pull request in a GitHub repository",
        InputSchema = """{"type":"object","properties":{"owner":{"type":"string"},"repo":{"type":"string"},"title":{"type":"string"},"head":{"type":"string"},"base":{"type":"string"},"body":{"type":"string"}},"required":["owner","repo","title","head","base"]}"""
    };

    public static readonly McpToolDefinition ListPullRequests = new()
    {
        Name = "github_list_pull_requests",
        Description = "Lists pull requests in a GitHub repository",
        InputSchema = """{"type":"object","properties":{"owner":{"type":"string"},"repo":{"type":"string"},"state":{"type":"string","enum":["open","closed","all"]}},"required":["owner","repo"]}"""
    };

    public static readonly McpToolDefinition GetRepository = new()
    {
        Name = "github_get_repository",
        Description = "Gets information about a GitHub repository",
        InputSchema = """{"type":"object","properties":{"owner":{"type":"string"},"repo":{"type":"string"}},"required":["owner","repo"]}"""
    };

    public static readonly McpToolDefinition SearchCode = new()
    {
        Name = "github_search_code",
        Description = "Searches for code across GitHub repositories",
        InputSchema = """{"type":"object","properties":{"query":{"type":"string"},"owner":{"type":"string"},"repo":{"type":"string"}},"required":["query"]}"""
    };

    public static IReadOnlyList<McpToolDefinition> All =>
    [
        CreateIssue, ListIssues, CreatePullRequest, ListPullRequests, GetRepository, SearchCode
    ];
}
