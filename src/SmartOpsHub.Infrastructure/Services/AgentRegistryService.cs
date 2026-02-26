using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Infrastructure.Services;

public sealed class AgentRegistryService : IAgentRegistry
{
    private static readonly IReadOnlyList<AgentDefinition> Agents =
    [
        new AgentDefinition
        {
            Id = "github-agent",
            Name = "GitHub Agent",
            Description = "Manages GitHub repositories, pull requests, issues, and workflows.",
            Type = AgentType.GitHub,
            SystemPrompt = "You are a GitHub operations assistant. Help users manage repositories, review pull requests, triage issues, and configure GitHub Actions workflows. Provide concise, actionable guidance using GitHub best practices."
        },
        new AgentDefinition
        {
            Id = "azure-agent",
            Name = "Azure Agent",
            Description = "Manages Azure cloud resources, deployments, and monitoring.",
            Type = AgentType.Azure,
            SystemPrompt = "You are an Azure cloud assistant. Help users provision and manage Azure resources, troubleshoot deployments, monitor services, and optimize cloud costs. Follow Azure Well-Architected Framework principles."
        },
        new AgentDefinition
        {
            Id = "ado-agent",
            Name = "Azure DevOps Agent",
            Description = "Manages Azure DevOps projects, pipelines, boards, and artifacts.",
            Type = AgentType.AzureDevOps,
            SystemPrompt = "You are an Azure DevOps assistant. Help users manage work items, configure build and release pipelines, organize boards, and manage artifacts. Follow DevOps best practices for CI/CD."
        },
        new AgentDefinition
        {
            Id = "dotnet-dev-agent",
            Name = ".NET Developer Agent",
            Description = "Assists with .NET development, code reviews, and architectural guidance.",
            Type = AgentType.DotNetDev,
            SystemPrompt = "You are a .NET development assistant. Help users write, review, and refactor C# and .NET code. Provide guidance on architecture, design patterns, testing, and performance optimization following current .NET best practices."
        },
        new AgentDefinition
        {
            Id = "ai-llm-agent",
            Name = "AI/LLM Agent",
            Description = "Assists with AI model integration, prompt engineering, and LLM operations.",
            Type = AgentType.AiLlm,
            SystemPrompt = "You are an AI and LLM operations assistant. Help users integrate AI models, craft effective prompts, manage model deployments, and implement responsible AI practices. Provide guidance on Azure OpenAI, semantic kernel, and AI orchestration patterns."
        },
        new AgentDefinition
        {
            Id = "devops-agent",
            Name = "DevOps Agent",
            Description = "Manages infrastructure as code, CI/CD pipelines, and platform engineering.",
            Type = AgentType.DevOps,
            SystemPrompt = "You are a DevOps and platform engineering assistant. Help users manage infrastructure as code, configure CI/CD pipelines, implement monitoring and observability, and follow SRE best practices for reliability and scalability."
        },
        new AgentDefinition
        {
            Id = "personal-agent",
            Name = "Personal Assistant Agent",
            Description = "Provides general productivity assistance, scheduling, and task management.",
            Type = AgentType.Personal,
            SystemPrompt = "You are a personal productivity assistant. Help users manage tasks, organize information, draft communications, and improve workflow efficiency. Be helpful, concise, and proactive in offering relevant suggestions."
        }
    ];

    public IReadOnlyList<AgentDefinition> GetAllAgents() => Agents;

    public AgentDefinition? GetAgent(AgentType type) =>
        Agents.FirstOrDefault(a => a.Type == type);

    public IReadOnlyList<AgentDefinition> GetAgentsForUser(UserProfile user) =>
        user.AssignedAgents.Count > 0
            ? Agents.Where(a => user.AssignedAgents.Contains(a.Type) && a.IsEnabled).ToList()
            : Agents.Where(a => a.IsEnabled).ToList();
}
