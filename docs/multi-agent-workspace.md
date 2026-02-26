# Multi-Agent Chat Workspace

## Concept Overview

The Multi-Agent Chat Workspace is a **command center with AI teammates**. Instead of a single chatbot, users see multiple agent panels side by side — each connected to a specialized AI agent backed by its own MCP server. When a user sends a message, selected agents process it concurrently and stream responses to their respective panels in real time.

Think of it as a mission control room where each screen shows a different specialist's perspective on the same question.

### Why Multi-Agent?

Operations teams deal with cross-cutting concerns:

- A deployment question might need input from Azure (infrastructure), ADO (pipeline status), and GitHub (code changes)
- A debugging session might involve .NET Dev (code analysis), DevOps (logs), and AI/LLM (reasoning)

Rather than switching between tools or asking one agent to do everything, the workspace shows all perspectives simultaneously.

## Agent Panel Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  Smart Ops Hub — Multi-Agent Workspace            [user menu]  │
├────────────┬────────────┬────────────┬────────────┬─────────────┤
│  GitHub    │  Azure     │  ADO       │  .NET Dev  │  DevOps     │
│  Agent     │  Agent     │  Agent     │  Agent     │  Agent      │
│            │            │            │            │             │
│ ┌────────┐ │ ┌────────┐ │ ┌────────┐ │ ┌────────┐ │ ┌────────┐  │
│ │You:    │ │ │You:    │ │ │You:    │ │ │You:    │ │ │You:    │  │
│ │What's  │ │ │What's  │ │ │What's  │ │ │What's  │ │ │What's  │  │
│ │the     │ │ │the     │ │ │the     │ │ │the     │ │ │the     │  │
│ │status? │ │ │status? │ │ │status? │ │ │status? │ │ │status? │  │
│ └────────┘ │ └────────┘ │ └────────┘ │ └────────┘ │ └────────┘  │
│ ┌────────┐ │ ┌────────┐ │ ┌────────┐ │ ┌────────┐ │ ┌────────┐  │
│ │Agent:  │ │ │Agent:  │ │ │Agent:  │ │ │Agent:  │ │ │Agent:  │  │
│ │PR #42  │ │ │3 VMs   │ │ │Build   │ │ │All     │ │ │Pipeline│  │
│ │merged  │ │ │healthy │ │ │passed  │ │ │tests   │ │ │#18 ran │  │
│ │today...│ │ │in US...│ │ │at 2pm..│ │ │pass... │ │ │clean...│  │
│ └────────┘ │ └────────┘ │ └────────┘ │ └────────┘ │ └────────┘  │
├────────────┴────────────┴────────────┴────────────┴─────────────┤
│  [Message input]                                    [Send All] │
└─────────────────────────────────────────────────────────────────┘
```

### Panel Features

Each agent panel includes:

- **Agent identity** — name, avatar, and status indicator (online/busy/error)
- **Chat history** — scrollable message history for that agent's session
- **Streaming response** — tokens appear as they arrive via SignalR
- **Tool call indicators** — shows when the agent is executing an MCP tool
- **Enable/disable toggle** — users can activate only the agents they need

## Concurrent Execution Model

### How It Works

```
User sends message
        │
        ▼
  ┌─────────────┐
  │  AgentHub    │  (SignalR on the API)
  │  receives    │
  │  message     │
  └──────┬──────┘
         │
         ├──────────────────────────────────────────┐
         │  Fan-out to enabled agents (concurrent)  │
         │                                          │
    ┌────▼────┐  ┌────▼────┐  ┌────▼────┐    ┌────▼────┐
    │ GitHub  │  │ Azure   │  │  ADO    │    │ DevOps  │
    │ Session │  │ Session │  │ Session │    │ Session │
    └────┬────┘  └────┬────┘  └────┬────┘    └────┬────┘
         │            │            │              │
    (each runs independently through the Orchestrator)
         │            │            │              │
    ┌────▼────┐  ┌────▼────┐  ┌────▼────┐    ┌────▼────┐
    │Response │  │Response │  │Response │    │Response │
    │streamed │  │streamed │  │streamed │    │streamed │
    │to panel │  │to panel │  │to panel │    │to panel │
    └─────────┘  └─────────┘  └─────────┘    └─────────┘
```

### Key Properties

- **Independent sessions** — each agent has its own `AgentSession` with separate chat history and context
- **No cross-talk** — agents don't see each other's responses (they focus on their domain)
- **Parallel execution** — all active agents process the message concurrently via `Task.WhenAll`
- **Independent streaming** — each agent streams tokens to its own SignalR channel
- **Fault isolation** — if one agent fails, others continue unaffected

### Session Lifecycle

```csharp
// Simplified flow (see IAgentOrchestrator)
var session = await orchestrator.CreateSessionAsync(userId, agentType);
var response = await orchestrator.ProcessMessageAsync(session.Id, userMessage);
// ... more messages ...
await orchestrator.EndSessionAsync(session.Id);
```

Each panel manages its own session. Sessions persist across page navigations but are cleaned up when explicitly ended or after inactivity.

## Agent-to-MCP Mapping

Each agent type maps 1:1 to an MCP server via the gateway:

| Agent Panel | `AgentType` Enum | MCP Server | Tools Available |
|---|---|---|---|
| GitHub | `GitHub` | `Mcp.GitHub` | Repos, PRs, issues, Actions, code search |
| Azure | `Azure` | `Mcp.Azure` | Resources, deployments, health, costs |
| Azure DevOps | `AzureDevOps` | `Mcp.Ado` | Pipelines, boards, work items, releases |
| .NET Dev | `DotNetDev` | `Mcp.DotNet` | Code analysis, NuGet, builds, scaffolding |
| AI/LLM | `AiLlm` | `Mcp.AI` | Model management, prompts, embeddings |
| DevOps | `DevOps` | `Mcp.DevOps` | CI/CD, monitoring, IaC operations |
| Personal | `Personal` | `Mcp.Personal` | User-defined plugins, notes, automations |

The mapping is defined in `AgentDefinition.McpServerEndpoint` — each agent definition includes the endpoint of its backing MCP server.

### Resolution Flow

```
AgentDefinition (Core model)
  │
  │  .McpServerEndpoint = "mcp-github"
  │
  ▼
IMcpGateway.GetClientAsync(AgentType.GitHub)
  │
  │  Resolves to internal endpoint
  │
  ▼
IMcpClient (connected to Mcp.GitHub server)
  │
  │  .ListToolsAsync() → available tools
  │  .CallToolAsync(toolCall) → result
```

## Configuring Agents Per Role

Agents can be configured and customized per user role to show only the panels relevant to that role.

### Agent Definition

Each agent is defined as an `AgentDefinition` record:

```csharp
new AgentDefinition
{
    Id = "github-agent",
    Name = "GitHub",
    Description = "Manages repositories, pull requests, and CI/CD workflows",
    Type = AgentType.GitHub,
    SystemPrompt = "You are a GitHub operations specialist...",
    AvatarUrl = "/images/github-avatar.png",
    McpServerEndpoint = "mcp-github",
    IsEnabled = true
};
```

### Role-Based Configuration

| Role | Active Agents |
|---|---|
| Platform Engineer | Azure, DevOps, ADO |
| Developer | GitHub, .NET Dev, AI/LLM, Personal |
| Release Manager | ADO, DevOps, GitHub |
| Full Access | All agents |

### Customization Points

- **System prompt** — tailor each agent's personality and focus area via `AgentDefinition.SystemPrompt`
- **Enable/disable** — toggle agents on or off with `AgentDefinition.IsEnabled`
- **MCP endpoint** — point an agent to a different MCP server instance (e.g., a staging server)
- **Avatar** — customize the agent's visual identity in the chat panel
- **Personal plugins** — extend the Personal agent with user-specific tools (see [MCP Servers — Personal Agent Plugin System](mcp-servers.md#personal-agent-plugin-system))
