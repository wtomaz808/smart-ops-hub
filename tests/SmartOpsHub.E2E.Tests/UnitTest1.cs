using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.E2E.Tests;

internal sealed class TestMcpGateway : IMcpGateway
{
    public Task<IMcpClient> GetClientAsync(AgentType agentType, CancellationToken ct = default)
        => Task.FromResult<IMcpClient>(new TestMcpClient());

    public Task<IReadOnlyDictionary<AgentType, bool>> GetHealthStatusAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<AgentType, bool>>(new Dictionary<AgentType, bool>());
}

internal sealed class TestMcpClient : IMcpClient
{
    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<McpToolDefinition>>([]);

    public Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken ct = default)
        => Task.FromResult(new McpToolResult { ToolCallId = toolCall.Id, Content = "test" });

    public Task<bool> IsHealthyAsync(CancellationToken ct = default) => Task.FromResult(true);
}

internal sealed class TestAiCompletionService : IAiCompletionService
{
    public Task<string> GetCompletionAsync(IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<McpToolDefinition>? availableTools = null, CancellationToken ct = default)
        => Task.FromResult("Test AI response");

    public async IAsyncEnumerable<string> StreamCompletionAsync(IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<McpToolDefinition>? availableTools = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return "Test ";
        yield return "response";
        await Task.CompletedTask;
    }
}

public sealed class SmartOpsHubTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace external services with test fakes
            var mcpDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IMcpGateway));
            if (mcpDesc is not null) services.Remove(mcpDesc);
            services.AddSingleton<IMcpGateway>(new TestMcpGateway());

            var aiDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IAiCompletionService));
            if (aiDesc is not null) services.Remove(aiDesc);
            services.AddSingleton<IAiCompletionService>(new TestAiCompletionService());
        });
    }
}

public class ApiSmokeTests : IClassFixture<SmartOpsHubTestFactory>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(SmartOpsHubTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_Endpoint_Returns_Ok()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_Ready_Endpoint_Returns_Ok()
    {
        var response = await _client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ListAgents_Returns_AllSevenAgents()
    {
        var response = await _client.GetAsync("/api/agents");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(7, json.GetArrayLength());
    }

    [Fact]
    public async Task ListAgents_EachAgent_HasRequiredFields()
    {
        var response = await _client.GetAsync("/api/agents");
        var agents = await response.Content.ReadFromJsonAsync<JsonElement>();

        foreach (var agent in agents.EnumerateArray())
        {
            Assert.True(agent.TryGetProperty("name", out _), "Agent missing 'name'");
            Assert.True(agent.TryGetProperty("type", out _), "Agent missing 'type'");
            Assert.True(agent.TryGetProperty("description", out _), "Agent missing 'description'");
        }
    }

    [Fact]
    public async Task CreateSession_Returns_Session()
    {
        var payload = new { UserId = "e2e-user", AgentType = 0 }; // GitHub = 0
        var response = await _client.PostAsJsonAsync("/api/sessions", payload);
        response.EnsureSuccessStatusCode();

        var session = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(session.TryGetProperty("sessionId", out _));
    }

    [Fact]
    public async Task CreateAndGetSession_Roundtrip()
    {
        var createPayload = new { UserId = "e2e-user", AgentType = 0 };
        var createResponse = await _client.PostAsJsonAsync("/api/sessions", createPayload);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = created.GetProperty("sessionId").GetString()!;

        var getResponse = await _client.GetAsync($"/api/sessions/{sessionId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetSession_NonExistent_Returns_NotFound()
    {
        var response = await _client.GetAsync("/api/sessions/nonexistent-id");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteSession_Returns_NoContent()
    {
        var createPayload = new { UserId = "e2e-user", AgentType = 0 };
        var createResponse = await _client.PostAsJsonAsync("/api/sessions", createPayload);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = created.GetProperty("sessionId").GetString()!;

        var deleteResponse = await _client.DeleteAsync($"/api/sessions/{sessionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/sessions/{sessionId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task SignalR_Hub_Endpoint_Exists()
    {
        // Verify the SignalR negotiate endpoint is reachable
        var response = await _client.PostAsync("/hubs/agent/negotiate?negotiateVersion=1", null);
        // SignalR negotiate returns 200 with connection info
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
