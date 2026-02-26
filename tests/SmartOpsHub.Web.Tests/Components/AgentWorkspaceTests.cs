using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Infrastructure.Services;

namespace SmartOpsHub.Web.Tests.Components;

public class AgentWorkspaceTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    [Fact]
    public void AgentRegistry_Returns_EnabledAgents()
    {
        var registry = new AgentRegistryService();
        var agents = registry.GetAllAgents();

        Assert.NotEmpty(agents);
        Assert.All(agents, a => Assert.True(a.IsEnabled));
    }

    [Fact]
    public void AgentRegistry_Returns_Seven_Agents()
    {
        var registry = new AgentRegistryService();
        var agents = registry.GetAllAgents();
        Assert.Equal(7, agents.Count);
    }

    [Fact]
    public void AgentRegistry_ContainsAllAgentTypes()
    {
        var registry = new AgentRegistryService();
        var types = registry.GetAllAgents().Select(a => a.Type).ToList();

        Assert.Contains(AgentType.GitHub, types);
        Assert.Contains(AgentType.Azure, types);
        Assert.Contains(AgentType.AzureDevOps, types);
        Assert.Contains(AgentType.DotNetDev, types);
        Assert.Contains(AgentType.AiLlm, types);
        Assert.Contains(AgentType.DevOps, types);
        Assert.Contains(AgentType.Personal, types);
    }

    [Fact]
    public void AgentRegistry_GetAgent_ReturnsCorrectAgent()
    {
        var registry = new AgentRegistryService();
        var agent = registry.GetAgent(AgentType.GitHub);

        Assert.NotNull(agent);
        Assert.Equal(AgentType.GitHub, agent!.Type);
        Assert.False(string.IsNullOrEmpty(agent.SystemPrompt));
    }

    [Fact]
    public void AgentRegistry_GetAgent_NonExistent_ReturnsNull()
    {
        var registry = new AgentRegistryService();
        var agent = registry.GetAgent((AgentType)999);
        Assert.Null(agent);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        GC.SuppressFinalize(this);
    }
}
