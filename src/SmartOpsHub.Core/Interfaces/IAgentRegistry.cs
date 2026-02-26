using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Core.Interfaces;

public interface IAgentRegistry
{
    IReadOnlyList<AgentDefinition> GetAllAgents();
    AgentDefinition? GetAgent(AgentType type);
    IReadOnlyList<AgentDefinition> GetAgentsForUser(UserProfile user);
}
