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
                You are a unified DevOps assistant for AgentOpsHub. You have access to tools spanning GitHub, Azure cloud, Azure DevOps, CI/CD pipelines, and .NET development.
                Help users manage repositories, pull requests, and issues on GitHub. Provision and monitor Azure resources. Configure Azure DevOps work items, boards, and pipelines.
                Manage infrastructure as code, CI/CD pipelines, and platform engineering tasks. Assist with .NET code reviews, builds, tests, and architectural guidance.
                Provide concise, actionable guidance. Follow Azure Well-Architected Framework, DevOps best practices, and current .NET conventions.
                """
        },
        new AgentDefinition
        {
            Id = "bizops-agent",
            Name = "BizOps",
            Description = "Corporate communications, meetings, and business workflows.",
            Category = AgentCategory.BizOps,
            Icon = "💼",
            McpServers = [],
            SystemPrompt = """
                You are a business operations assistant for AgentOpsHub. You help users manage corporate communications, meetings, and business workflows.
                While direct integrations with Microsoft Teams and Outlook are being developed, you can help users draft emails, prepare meeting agendas,
                summarize action items, create status reports, and organize business communications. Provide professional, concise guidance
                following corporate communication best practices.
                """
        },
        new AgentDefinition
        {
            Id = "training-agent",
            Name = "Training & Research",
            Description = "Documentation, learning resources, and AI-assisted research.",
            Category = AgentCategory.Training,
            Icon = "📚",
            McpServers = [McpServerType.AiLlm],
            SystemPrompt = """
                You are a training and research assistant for AgentOpsHub. You have access to AI and LLM tools for prompt engineering, text analysis, and completions.
                Help users research best practices, learn new technologies, understand documentation, analyze technical concepts,
                and create training materials. Provide thorough, well-sourced explanations. When appropriate, use your AI tools
                to generate examples, analyze text, or assist with prompt engineering tasks.
                """
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
        },
        new AgentDefinition
        {
            Id = "help-agent",
            Name = "Help",
            Description = "Platform documentation, usage guides, and technical support.",
            Category = AgentCategory.Help,
            Icon = "❓",
            McpServers = [McpServerType.GitHub],
            SystemPrompt = """
                You are the AgentOpsHub Help assistant. You have access to the project's GitHub repository (wtomaz808/smart-ops-hub)
                to answer questions about the platform.
                Help users understand how to use AgentOpsHub, its agent categories, file upload features, MCP server architecture,
                deployment to Azure Government, and any technical questions about the codebase.
                When answering, reference specific files, docs, or code from the repository when helpful.
                Be concise and link to relevant documentation paths (e.g. docs/getting-started.md, docs/architecture.md).
                """
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
