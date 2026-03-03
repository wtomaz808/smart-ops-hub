using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Infrastructure.Services;

public sealed class AgentRegistryService : IAgentRegistry
{
    private static readonly IReadOnlyList<AgentDefinition> Agents =
    [
        new AgentDefinition
        {
            Id = "devops-agent",
            Name = "DevOps",
            Description = "GitHub, Azure, CI/CD pipelines, .NET development, and infrastructure operations.",
            Category = AgentCategory.DevOps,
            Icon = "⚙️",
            McpServers = [McpServerType.GitHub, McpServerType.Azure, McpServerType.AzureDevOps, McpServerType.DevOps, McpServerType.DotNetDev],
            SystemPrompt = """
                You are a unified DevOps assistant for Smart Ops Hub. You have access to tools spanning GitHub, Azure cloud, Azure DevOps, CI/CD pipelines, and .NET development.
                Help users manage repositories, pull requests, and issues on GitHub. Provision and monitor Azure resources. Configure Azure DevOps work items, boards, and pipelines.
                Manage infrastructure as code, CI/CD pipelines, and platform engineering tasks. Assist with .NET code reviews, builds, tests, and architectural guidance.
                Provide concise, actionable guidance. Follow Azure Well-Architected Framework, DevOps best practices, and current .NET conventions.
                """
        },
        new AgentDefinition
        {
            Id = "bizops-agent",
            Name = "BizOps",
            Description = "Teams, Outlook, and corporate communication tools.",
            Category = AgentCategory.BizOps,
            Icon = "💼",
            McpServers = [],
            IsComingSoon = true,
            SystemPrompt = "You are a business operations assistant. Help users manage corporate communications, meetings, and workflows through Microsoft Teams and Outlook."
        },
        new AgentDefinition
        {
            Id = "training-agent",
            Name = "Training & Research",
            Description = "Documentation, web search, and learning resources.",
            Category = AgentCategory.Training,
            Icon = "📚",
            McpServers = [],
            IsComingSoon = true,
            SystemPrompt = "You are a training and research assistant. Help users find documentation, research best practices, and discover learning resources."
        },
        new AgentDefinition
        {
            Id = "personal-agent",
            Name = "Personal",
            Description = "Productivity, calendar, and personal tools.",
            Category = AgentCategory.Personal,
            Icon = "🎯",
            McpServers = [McpServerType.Personal],
            SystemPrompt = "You are a personal productivity assistant. Help users manage tasks, organize information, check calendars, and improve workflow efficiency. Be helpful, concise, and proactive."
        }
    ];

    public IReadOnlyList<AgentDefinition> GetAllAgents() => Agents;

    public AgentDefinition? GetAgent(AgentCategory category) =>
        Agents.FirstOrDefault(a => a.Category == category);

    public IReadOnlyList<AgentDefinition> GetAgentsForUser(UserProfile user) =>
        user.AssignedAgents.Count > 0
            ? Agents.Where(a => user.AssignedAgents.Contains(a.Category) && a.IsEnabled).ToList()
            : Agents.Where(a => a.IsEnabled).ToList();
}
