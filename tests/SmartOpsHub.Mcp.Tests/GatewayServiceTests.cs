using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.Gateway.Services;
using SmartOpsHub.Mcp.Personal;
using SmartOpsHub.Mcp.Personal.Plugins;

namespace SmartOpsHub.Mcp.Tests;

public class McpGatewayServiceTests
{
    private static McpGatewayService CreateGateway()
    {
        // Use PersonalMcpClient as a lightweight test client
        var personalClient = new PersonalMcpClient([new CalendarPlugin()]);
        var clients = new List<KeyValuePair<AgentType, IMcpClient>>
        {
            new(AgentType.Personal, personalClient)
        };
        return new McpGatewayService(clients);
    }

    [Fact]
    public async Task GetClientAsync_RegisteredAgent_Returns_Client()
    {
        var gateway = CreateGateway();
        var client = await gateway.GetClientAsync(AgentType.Personal);
        Assert.NotNull(client);
    }

    [Fact]
    public async Task GetClientAsync_UnregisteredAgent_Throws()
    {
        var gateway = CreateGateway();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => gateway.GetClientAsync(AgentType.GitHub));
    }

    [Fact]
    public async Task GetHealthStatusAsync_Returns_StatusForAllClients()
    {
        var gateway = CreateGateway();
        var status = await gateway.GetHealthStatusAsync();
        Assert.Single(status);
        Assert.True(status[AgentType.Personal]);
    }

    [Fact]
    public async Task GetClientAsync_Returns_FunctionalClient()
    {
        var gateway = CreateGateway();
        var client = await gateway.GetClientAsync(AgentType.Personal);
        var tools = await client.ListToolsAsync();
        Assert.True(tools.Count > 0);
    }
}
