using System.Net;
using System.Text.Json;

namespace SmartOpsHub.Api.Tests;

public class AgentEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AgentEndpointTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAgents_ReturnsListOfAgents()
    {
        var response = await _client.GetAsync("/api/agents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var agents = JsonSerializer.Deserialize<JsonElement[]>(content, JsonOptions);

        Assert.NotNull(agents);
    }

    [Fact]
    public async Task GetAgents_ListIsNotEmpty()
    {
        var response = await _client.GetAsync("/api/agents");
        var content = await response.Content.ReadAsStringAsync();
        var agents = JsonSerializer.Deserialize<JsonElement[]>(content, JsonOptions);

        Assert.NotNull(agents);
        Assert.NotEmpty(agents);
    }

    [Fact]
    public async Task GetAgents_EachAgentHasRequiredProperties()
    {
        var response = await _client.GetAsync("/api/agents");
        var content = await response.Content.ReadAsStringAsync();
        var agents = JsonSerializer.Deserialize<JsonElement[]>(content, JsonOptions);

        Assert.NotNull(agents);
        foreach (var agent in agents)
        {
            Assert.True(agent.TryGetProperty("name", out var name), "Agent missing 'name' property");
            Assert.False(string.IsNullOrEmpty(name.GetString()));

            Assert.True(agent.TryGetProperty("type", out _), "Agent missing 'type' property");

            Assert.True(agent.TryGetProperty("description", out var desc), "Agent missing 'description' property");
            Assert.False(string.IsNullOrEmpty(desc.GetString()));
        }
    }
}
