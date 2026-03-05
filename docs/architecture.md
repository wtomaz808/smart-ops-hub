# Architecture

## System Overview

AgentOpsHub is structured as a three-tier application deployed entirely on **Azure Container Apps** in Azure Government:

1. **Blazor Server frontend** — interactive SSR UI with multi-agent chat panels
2. **ASP.NET Core API** — Minimal API with SignalR `AgentHub`, Agent Orchestrator, and Azure OpenAI integration
3. **MCP server layer** — 7 specialized MCP servers behind a gateway, each exposing domain-specific tools

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Azure Government                           │
│                                                                     │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────────────┐    │
│  │  Entra ID    │   │  Azure SQL   │   │  Azure OpenAI        │    │
│  │  (Auth)      │   │  Database    │   │  (GPT-4o / Embeddings)│   │
│  └──────┬───────┘   └──────┬───────┘   └──────────┬───────────┘    │
│         │                  │                       │                │
│  ┌──────▼──────────────────▼───────────────────────▼───────────┐   │
│  │                    Container App Env                         │   │
│  │  ┌─────────┐   ┌─────────┐   ┌───────────────────────────┐ │   │
│  │  │   Web   │──▶│   API   │──▶│       MCP Gateway         │ │   │
│  │  │ (Blazor)│   │(Minimal │   │  ┌───┬───┬───┬───┬───┬──┐ │ │   │
│  │  │         │   │  API +  │   │  │GH │AZ │ADO│.NT│AI │DO│ │ │   │
│  │  │         │   │ SignalR)│   │  └───┴───┴───┴───┴───┴──┘ │ │   │
│  │  └─────────┘   └─────────┘   │  ┌──────────────────────┐ │ │   │
│  │                              │  │   Personal Server     │ │ │   │
│  │                              │  └──────────────────────┘ │ │   │
│  │                              └───────────────────────────┘ │   │
│  └────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

## Multi-Agent Chat Workspace

### Design Principles

The workspace provides a **command-center** experience: multiple AI agents visible simultaneously, each specialized for a domain, all responding to the same user query in parallel.

### Component Interaction

```
User types message
        │
        ▼
┌───────────────┐     SignalR      ┌───────────────────┐
│  Blazor Agent │ ────────────────▶│   API AgentHub    │
│  Chat Panels  │                  │                   │
│  (per agent)  │ ◀────────────────│  (streams tokens) │
└───────────────┘     SignalR      └────────┬──────────┘
                                            │
                                            ▼
                                   ┌────────────────────┐
                                   │ Agent Orchestrator  │
                                   │                    │
                                   │ 1. Route to agents │
                                   │ 2. Call Azure AOAI │
                                   │ 3. Execute MCP     │
                                   │    tool calls      │
                                   │ 4. Stream results  │
                                   └────────┬───────────┘
                                            │
                                   ┌────────▼───────────┐
                                   │    MCP Gateway      │
                                   │  (routes to server) │
                                   └────────────────────┘
```

### Agent Types

| Agent | Type Enum | MCP Server | Purpose |
|---|---|---|---|
| GitHub Agent | `GitHub` | `Mcp.GitHub` | Repos, PRs, issues, actions |
| Azure Agent | `Azure` | `Mcp.Azure` | Azure resources, deployments |
| ADO Agent | `AzureDevOps` | `Mcp.Ado` | Pipelines, boards, artifacts |
| .NET Dev Agent | `DotNetDev` | `Mcp.DotNet` | Code analysis, NuGet, builds |
| AI/LLM Agent | `AiLlm` | `Mcp.AI` | Model management, prompt tools |
| DevOps Agent | `DevOps` | `Mcp.DevOps` | CI/CD, IaC, monitoring |
| Personal Agent | `Personal` | `Mcp.Personal` | Per-user plugins, notes |

### SignalR Hub

The `AgentHub` on the API server manages real-time connections:

- Client joins a session → hub tracks connection per agent panel
- User message → Orchestrator fans out to selected agents concurrently
- Each agent's response tokens stream back to the client's specific panel
- Panel UI renders tokens as they arrive (typewriter effect)

## MCP Server Architecture

### Protocol

All MCP servers implement the [Model Context Protocol](https://modelcontextprotocol.io/) — a standard for LLM-to-tool communication. Each server exposes:

- **Tool definitions** — schema describing available operations
- **Tool execution** — accepts structured arguments, returns results
- **Health endpoint** — `/health` for gateway routing and monitoring

### Gateway

The MCP Gateway (`SmartOpsHub.Mcp.Gateway`) is the single entry point for the API layer:

```
API  ──▶  MCP Gateway  ──▶  GitHub Server
                       ──▶  Azure Server
                       ──▶  ADO Server
                       ──▶  DotNet Server
                       ──▶  AI Server
                       ──▶  DevOps Server
                       ──▶  Personal Server
```

Routing is based on `AgentType` — the gateway resolves the target server endpoint and forwards the MCP request. It also aggregates health status across all servers.

### Containerization

Each MCP server is an independent ASP.NET Core application packaged as a container image and deployed as its own Azure Container App revision. This enables:

- Independent scaling per server
- Zero-downtime deployments
- Isolated failure domains

## Managed Identity Strategy

AgentOpsHub uses **Azure Managed Identities** exclusively for service-to-service authentication — no passwords or connection strings with secrets are stored:

| Connection | Auth Method |
|---|---|
| API → Azure SQL | System-assigned managed identity |
| API → Azure OpenAI | System-assigned managed identity |
| API → MCP Gateway | Internal Container App networking |
| MCP servers → Azure services | System-assigned managed identity |
| Users → Web | Microsoft Entra ID (MSAL) |

## Data Flow

### User Message Processing

```
1. User sends message in chat panel
              │
2. Blazor ──SignalR──▶ API AgentHub
              │
3. AgentHub ──▶ Agent Orchestrator
              │
4. Orchestrator ──▶ Azure OpenAI (reasoning + tool selection)
              │
5. If tool call needed:
   Orchestrator ──▶ MCP Gateway ──▶ Target MCP Server
              │
6. MCP Server executes tool, returns result
              │
7. Orchestrator ──▶ Azure OpenAI (incorporate tool result)
              │
8. Azure OpenAI streams completion tokens
              │
9. Orchestrator ──SignalR──▶ Blazor panel (streamed)
```

### Concurrent Multi-Agent Flow

When a user message targets multiple agents:

```
User message: "What's the status of our deployment?"
              │
              ├──▶ Azure Agent  ──▶ Mcp.Azure  ──▶ "3 resources healthy"
              ├──▶ DevOps Agent ──▶ Mcp.DevOps ──▶ "Pipeline #42 succeeded"
              └──▶ ADO Agent    ──▶ Mcp.Ado    ──▶ "Release 2.1 approved"
              │
   All responses stream to their respective panels simultaneously
```

## Azure Government Deployment Topology

```
Azure Gov Region (e.g., USGov Virginia)
├── Resource Group: rg-smartopshub
│   ├── Container App Environment
│   │   ├── Container App: smartopshub-web        (Blazor Server)
│   │   ├── Container App: smartopshub-api        (Minimal API + SignalR)
│   │   ├── Container App: smartopshub-mcp-gw     (MCP Gateway)
│   │   ├── Container App: smartopshub-mcp-github
│   │   ├── Container App: smartopshub-mcp-azure
│   │   ├── Container App: smartopshub-mcp-ado
│   │   ├── Container App: smartopshub-mcp-dotnet
│   │   ├── Container App: smartopshub-mcp-ai
│   │   ├── Container App: smartopshub-mcp-devops
│   │   └── Container App: smartopshub-mcp-personal
│   ├── Azure SQL Server + Database
│   ├── Azure OpenAI Service
│   ├── Azure Container Registry
│   ├── Log Analytics Workspace
│   └── Key Vault (certificates only — no runtime secrets)
```

All inter-container traffic stays within the Container App Environment's internal virtual network. Only the Web container app has an external ingress endpoint.
