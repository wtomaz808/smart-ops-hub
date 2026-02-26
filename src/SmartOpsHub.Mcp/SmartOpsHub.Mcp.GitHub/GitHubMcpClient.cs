using System.Text.Json;
using Microsoft.Extensions.Logging;
using Octokit;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.GitHub.Tools;

namespace SmartOpsHub.Mcp.GitHub;

public sealed partial class GitHubMcpClient(GitHubClient github, ILogger<GitHubMcpClient> logger) : IMcpClient
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(GitHubToolDefinitions.All);

    public async Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken cancellationToken = default)
    {
        try
        {
            return toolCall.ToolName switch
            {
                "github_create_issue" => await HandleCreateIssueAsync(toolCall),
                "github_list_issues" => await HandleListIssuesAsync(toolCall),
                "github_create_pull_request" => await HandleCreatePullRequestAsync(toolCall),
                "github_list_pull_requests" => await HandleListPullRequestsAsync(toolCall),
                "github_get_repository" => await HandleGetRepositoryAsync(toolCall),
                "github_search_code" => await HandleSearchCodeAsync(toolCall),
                _ => new McpToolResult { ToolCallId = toolCall.Id, Content = $"Unknown tool: {toolCall.ToolName}", IsError = true }
            };
        }
        catch (Exception ex)
        {
            LogToolError(ex, toolCall.ToolName);
            return new McpToolResult { ToolCallId = toolCall.Id, Content = $"GitHub API error: {ex.Message}", IsError = true };
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var limits = await github.RateLimit.GetRateLimits();
            return limits.Resources.Core.Remaining > 0;
        }
        catch (Exception ex)
        {
            LogHealthCheckFailed(ex);
            return false;
        }
    }

    private async Task<McpToolResult> HandleCreateIssueAsync(McpToolCall toolCall)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var owner = args.GetProperty("owner").GetString()!;
        var repo = args.GetProperty("repo").GetString()!;
        var title = args.GetProperty("title").GetString()!;

        var newIssue = new NewIssue(title);
        if (args.TryGetProperty("body", out var body) && body.ValueKind == JsonValueKind.String)
            newIssue.Body = body.GetString();
        if (args.TryGetProperty("labels", out var labels) && labels.ValueKind == JsonValueKind.Array)
        {
            foreach (var label in labels.EnumerateArray())
                newIssue.Labels.Add(label.GetString()!);
        }

        var issue = await github.Issue.Create(owner, repo, newIssue);
        LogToolExecuted("github_create_issue", owner, repo);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = JsonSerializer.Serialize(new
            {
                issue.Id, issue.Number, issue.Title,
                State = issue.State.StringValue, HtmlUrl = issue.HtmlUrl
            }, s_jsonOptions)
        };
    }

    private async Task<McpToolResult> HandleListIssuesAsync(McpToolCall toolCall)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var owner = args.GetProperty("owner").GetString()!;
        var repo = args.GetProperty("repo").GetString()!;

        var request = new RepositoryIssueRequest();
        if (args.TryGetProperty("state", out var state) && state.ValueKind == JsonValueKind.String)
        {
            request.State = state.GetString() switch
            {
                "closed" => ItemStateFilter.Closed,
                "all" => ItemStateFilter.All,
                _ => ItemStateFilter.Open
            };
        }

        var issues = await github.Issue.GetAllForRepository(owner, repo, request);
        var result = issues.Select(i => new
        {
            i.Id, i.Number, i.Title,
            State = i.State.StringValue, HtmlUrl = i.HtmlUrl
        }).ToList();

        LogToolExecuted("github_list_issues", owner, repo);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = JsonSerializer.Serialize(result, s_jsonOptions)
        };
    }

    private async Task<McpToolResult> HandleCreatePullRequestAsync(McpToolCall toolCall)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var owner = args.GetProperty("owner").GetString()!;
        var repo = args.GetProperty("repo").GetString()!;
        var title = args.GetProperty("title").GetString()!;
        var head = args.GetProperty("head").GetString()!;
        var baseBranch = args.GetProperty("base").GetString()!;

        var newPr = new NewPullRequest(title, head, baseBranch);
        if (args.TryGetProperty("body", out var body) && body.ValueKind == JsonValueKind.String)
            newPr.Body = body.GetString();

        var pr = await github.PullRequest.Create(owner, repo, newPr);
        LogToolExecuted("github_create_pull_request", owner, repo);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = JsonSerializer.Serialize(new
            {
                pr.Id, pr.Number, pr.Title,
                State = pr.State.StringValue, HtmlUrl = pr.HtmlUrl
            }, s_jsonOptions)
        };
    }

    private async Task<McpToolResult> HandleListPullRequestsAsync(McpToolCall toolCall)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var owner = args.GetProperty("owner").GetString()!;
        var repo = args.GetProperty("repo").GetString()!;

        var request = new PullRequestRequest();
        if (args.TryGetProperty("state", out var state) && state.ValueKind == JsonValueKind.String)
        {
            request.State = state.GetString() switch
            {
                "closed" => ItemStateFilter.Closed,
                "all" => ItemStateFilter.All,
                _ => ItemStateFilter.Open
            };
        }

        var prs = await github.PullRequest.GetAllForRepository(owner, repo, request);
        var result = prs.Select(p => new
        {
            p.Id, p.Number, p.Title,
            State = p.State.StringValue, HtmlUrl = p.HtmlUrl
        }).ToList();

        LogToolExecuted("github_list_pull_requests", owner, repo);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = JsonSerializer.Serialize(result, s_jsonOptions)
        };
    }

    private async Task<McpToolResult> HandleGetRepositoryAsync(McpToolCall toolCall)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var owner = args.GetProperty("owner").GetString()!;
        var repo = args.GetProperty("repo").GetString()!;

        var repository = await github.Repository.Get(owner, repo);
        LogToolExecuted("github_get_repository", owner, repo);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = JsonSerializer.Serialize(new
            {
                repository.Id, repository.Name, repository.FullName,
                repository.Description, HtmlUrl = repository.HtmlUrl,
                repository.Language, repository.StargazersCount,
                repository.ForksCount, repository.OpenIssuesCount,
                repository.DefaultBranch, repository.Private
            }, s_jsonOptions)
        };
    }

    private async Task<McpToolResult> HandleSearchCodeAsync(McpToolCall toolCall)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var query = args.GetProperty("query").GetString()!;

        var searchRequest = new SearchCodeRequest(query);
        if (args.TryGetProperty("owner", out var ownerEl) && args.TryGetProperty("repo", out var repoEl)
            && ownerEl.ValueKind == JsonValueKind.String && repoEl.ValueKind == JsonValueKind.String)
        {
            var ownerStr = ownerEl.GetString();
            var repoStr = repoEl.GetString();
            if (!string.IsNullOrEmpty(ownerStr) && !string.IsNullOrEmpty(repoStr))
                searchRequest.Repos = new RepositoryCollection { $"{ownerStr}/{repoStr}" };
        }

        var results = await github.Search.SearchCode(searchRequest);
        var items = results.Items.Select(i => new
        {
            i.Name, i.Path,
            Repository = i.Repository.FullName, HtmlUrl = i.HtmlUrl
        }).ToList();

        LogToolExecuted("github_search_code", query, "");
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            Content = JsonSerializer.Serialize(new { results.TotalCount, Items = items }, s_jsonOptions)
        };
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executed {ToolName} for {Owner}/{Repo}")]
    private partial void LogToolExecuted(string toolName, string owner, string repo);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error executing GitHub tool {ToolName}")]
    private partial void LogToolError(Exception ex, string toolName);

    [LoggerMessage(Level = LogLevel.Warning, Message = "GitHub health check failed")]
    private partial void LogHealthCheckFailed(Exception ex);
}
