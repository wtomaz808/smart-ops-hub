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
    public void AgentRegistry_Returns_Five_Agents()
    {
        var registry = new AgentRegistryService();
        var agents = registry.GetAllAgents();
        Assert.Equal(5, agents.Count);
    }

    [Fact]
    public void AgentRegistry_ContainsAllAgentCategories()
    {
        var registry = new AgentRegistryService();
        var categories = registry.GetAllAgents().Select(a => a.Category).ToList();

        Assert.Contains(AgentCategory.DevOps, categories);
        Assert.Contains(AgentCategory.BizOps, categories);
        Assert.Contains(AgentCategory.Training, categories);
        Assert.Contains(AgentCategory.Personal, categories);
    }

    [Fact]
    public void AgentRegistry_GetAgent_ReturnsCorrectAgent()
    {
        var registry = new AgentRegistryService();
        var agent = registry.GetAgent(AgentCategory.DevOps);

        Assert.NotNull(agent);
        Assert.Equal(AgentCategory.DevOps, agent!.Category);
        Assert.False(string.IsNullOrEmpty(agent.SystemPrompt));
    }

    [Fact]
    public void AgentRegistry_GetAgent_NonExistent_ReturnsNull()
    {
        var registry = new AgentRegistryService();
        var agent = registry.GetAgent((AgentCategory)999);
        Assert.Null(agent);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        GC.SuppressFinalize(this);
    }
}
