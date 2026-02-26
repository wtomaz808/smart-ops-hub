using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Octokit;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.GitHub;
using SmartOpsHub.Mcp.GitHub.Tools;

namespace SmartOpsHub.Mcp.Tests;

public class GitHubToolDefinitionsTests
{
    [Fact]
    public void All_Returns_Six_Tools()
    {
        var tools = GitHubToolDefinitions.All;
        Assert.Equal(6, tools.Count);
    }

    [Theory]
    [InlineData("github_create_issue")]
    [InlineData("github_list_issues")]
    [InlineData("github_create_pull_request")]
    [InlineData("github_list_pull_requests")]
    [InlineData("github_get_repository")]
    [InlineData("github_search_code")]
    public void All_Contains_Tool(string toolName)
    {
        var tools = GitHubToolDefinitions.All;
        Assert.Contains(tools, t => t.Name == toolName);
    }

    [Fact]
    public void All_Tools_Have_Description_And_Schema()
    {
        foreach (var tool in GitHubToolDefinitions.All)
        {
            Assert.False(string.IsNullOrEmpty(tool.Name));
            Assert.False(string.IsNullOrEmpty(tool.Description));
            Assert.False(string.IsNullOrEmpty(tool.InputSchema));

            // Verify schema is valid JSON
            var doc = JsonDocument.Parse(tool.InputSchema!);
            Assert.Equal("object", doc.RootElement.GetProperty("type").GetString());
            Assert.True(doc.RootElement.TryGetProperty("required", out _));
        }
    }

    [Fact]
    public void CreateIssue_Schema_Requires_Owner_Repo_Title()
    {
        var schema = JsonDocument.Parse(GitHubToolDefinitions.CreateIssue.InputSchema!).RootElement;
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("owner", required);
        Assert.Contains("repo", required);
        Assert.Contains("title", required);
    }

    [Fact]
    public void CreatePullRequest_Schema_Requires_Head_And_Base()
    {
        var schema = JsonDocument.Parse(GitHubToolDefinitions.CreatePullRequest.InputSchema!).RootElement;
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("head", required);
        Assert.Contains("base", required);
    }
}

public class GitHubMcpClientTests
{
    private static GitHubMcpClient CreateClient()
    {
        var github = new GitHubClient(new ProductHeaderValue("SmartOpsHub-Tests"));
        return new GitHubMcpClient(github, NullLogger<GitHubMcpClient>.Instance);
    }

    [Fact]
    public async Task ListToolsAsync_Returns_All_Tools()
    {
        var client = CreateClient();
        var tools = await client.ListToolsAsync();
        Assert.Equal(6, tools.Count);
    }

    [Fact]
    public async Task ExecuteToolAsync_UnknownTool_Returns_Error()
    {
        var client = CreateClient();
        var toolCall = new McpToolCall { ToolName = "github_unknown", Arguments = "{}" };

        var result = await client.ExecuteToolAsync(toolCall);

        Assert.True(result.IsError);
        Assert.Contains("Unknown tool", result.Content);
    }

    [Fact]
    public async Task ExecuteToolAsync_InvalidArguments_Returns_Error()
    {
        var client = CreateClient();
        var toolCall = new McpToolCall { ToolName = "github_create_issue", Arguments = "not-json" };

        var result = await client.ExecuteToolAsync(toolCall);

        Assert.True(result.IsError);
        Assert.Contains("error", result.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteToolAsync_MissingRequiredField_Returns_Error()
    {
        var client = CreateClient();
        // Missing "owner" and "repo" required fields
        var toolCall = new McpToolCall
        {
            ToolName = "github_create_issue",
            Arguments = """{"title": "test"}"""
        };

        var result = await client.ExecuteToolAsync(toolCall);

        Assert.True(result.IsError);
    }
}
