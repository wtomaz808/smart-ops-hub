using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Core.Tests.Models;

public class AgentDefinitionTests
{
    [Fact]
    public void RequiredProperties_AreSet()
    {
        var agent = new AgentDefinition
        {
            Id = "test-agent",
            Name = "Test Agent",
            Description = "A test agent",
            Type = AgentType.GitHub,
            SystemPrompt = "You are a test agent."
        };

        Assert.Equal("test-agent", agent.Id);
        Assert.Equal("Test Agent", agent.Name);
        Assert.Equal("A test agent", agent.Description);
        Assert.Equal(AgentType.GitHub, agent.Type);
        Assert.Equal("You are a test agent.", agent.SystemPrompt);
    }

    [Fact]
    public void IsEnabled_DefaultsToTrue()
    {
        var agent = new AgentDefinition
        {
            Id = "test-agent",
            Name = "Test Agent",
            Description = "A test agent",
            Type = AgentType.GitHub,
            SystemPrompt = "You are a test agent."
        };

        Assert.True(agent.IsEnabled);
    }
}
