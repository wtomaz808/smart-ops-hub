using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Core.Interfaces;

public interface IAgentRegistry
{
    IReadOnlyList<AgentDefinition> GetAllAgents();
    AgentDefinition? GetAgent(AgentCategory category);
    IReadOnlyList<AgentDefinition> GetAgentsForUser(UserProfile user);
}
