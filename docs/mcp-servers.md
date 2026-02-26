# MCP Servers

## What is MCP?

**Model Context Protocol (MCP)** is an open standard for LLM-to-tool communication. It defines how an AI model discovers available tools, invokes them with structured arguments, and receives results — enabling AI agents to take actions beyond text generation.

In Smart Ops Hub, each MCP server exposes domain-specific tools that the Agent Orchestrator can call during a conversation. When Azure OpenAI determines that a user's request requires an action (e.g., "list my Azure resources"), it generates a tool call that the Orchestrator routes through the MCP Gateway to the appropriate server.

### MCP Core Concepts

```
┌──────────────┐       ┌──────────────┐       ┌──────────────┐
│   AI Model   │──────▶│  MCP Client  │──────▶│  MCP Server  │
│ (Azure AOAI) │       │ (Orchestrator)│      │  (Tool Host) │
│              │◀──────│              │◀──────│              │
│  Tool Call   │       │  Forward     │       │  Execute &   │
│  Decision    │       │  Request     │       │  Return      │
└──────────────┘       └──────────────┘       └──────────────┘
```

- **Tool Definition** — a schema describing a tool's name, description, and input parameters
- **Tool Call** — a structured request with tool name and arguments
- **Tool Result** — the output returned after executing the tool

These are modeled in the codebase as `McpToolDefinition`, `McpToolCall`, and `McpToolResult` in `SmartOpsHub.Core.Models`.

## MCP Server Inventory

Smart Ops Hub includes **7 specialized MCP servers** plus a **gateway**:

| # | Server | Project | Description |
|---|---|---|---|
| 1 | **GitHub** | `SmartOpsHub.Mcp.GitHub` | Repository management, pull requests, issues, Actions workflows, code search |
| 2 | **Azure** | `SmartOpsHub.Mcp.Azure` | Azure resource management, deployments, resource health, cost queries |
| 3 | **Azure DevOps** | `SmartOpsHub.Mcp.Ado` | Pipelines, boards, work items, artifacts, release management |
| 4 | **.NET** | `SmartOpsHub.Mcp.DotNet` | Code analysis, NuGet package management, build tooling, project scaffolding |
| 5 | **AI/LLM** | `SmartOpsHub.Mcp.AI` | Model management, prompt engineering tools, embedding utilities |
| 6 | **DevOps** | `SmartOpsHub.Mcp.DevOps` | CI/CD orchestration, infrastructure monitoring, IaC operations |
| 7 | **Personal** | `SmartOpsHub.Mcp.Personal` | Per-user agent plugins, notes, bookmarks, custom automations |
| — | **Gateway** | `SmartOpsHub.Mcp.Gateway` | Routes MCP requests to the correct server, aggregates health |

## Containerization and Deployment

Each MCP server is an independent ASP.NET Core application:

```
src/SmartOpsHub.Mcp/
├── SmartOpsHub.Mcp.Gateway/     # Entry point for all MCP traffic
├── SmartOpsHub.Mcp.GitHub/
├── SmartOpsHub.Mcp.Azure/
├── SmartOpsHub.Mcp.Ado/
├── SmartOpsHub.Mcp.DotNet/
├── SmartOpsHub.Mcp.AI/
├── SmartOpsHub.Mcp.DevOps/
└── SmartOpsHub.Mcp.Personal/
```

### Container Strategy

- Each server builds as its own container image
- Deployed as individual Azure Container Apps in the shared Container App Environment
- Servers communicate internally within the environment's virtual network
- Only the Gateway receives traffic from the API — individual servers are not directly exposed

### Scaling

Container Apps auto-scale each server independently based on HTTP request volume. Servers with bursty usage patterns (e.g., GitHub during PR reviews) scale up without affecting others.

## Gateway Routing

The MCP Gateway (`SmartOpsHub.Mcp.Gateway`) is the single entry point from the API layer. It:

1. **Receives** an MCP request with an `AgentType` identifier
2. **Resolves** the target server's internal endpoint
3. **Forwards** the MCP protocol request
4. **Returns** the result to the API

### Routing Flow

```
API (Agent Orchestrator)
  │
  │  POST /mcp/tools/call
  │  { agentType: "GitHub", toolName: "list_repos", args: {...} }
  │
  ▼
MCP Gateway
  │
  │  Resolve: GitHub → http://smartopshub-mcp-github:8080
  │
  ▼
GitHub MCP Server
  │
  │  Execute tool, return result
  │
  ▼
MCP Gateway → API → SignalR → Blazor Panel
```

### Health Aggregation

The gateway exposes a `/health` endpoint and also polls each server's health:

```
GET /health
{
  "status": "Healthy",
  "servers": {
    "GitHub": "Healthy",
    "Azure": "Healthy",
    "Ado": "Healthy",
    "DotNet": "Healthy",
    "AI": "Healthy",
    "DevOps": "Healthy",
    "Personal": "Healthy"
  }
}
```

The `IMcpGateway.GetHealthStatusAsync()` interface method returns this status map to the API, which uses it to indicate server availability in the UI.

## Personal Agent Plugin System

The **Personal MCP Server** (`SmartOpsHub.Mcp.Personal`) is unique — it supports per-user customization:

### Concept

Each user can register personal "plugins" — custom tools that extend their Personal agent. This enables:

- **Custom automations** — scripts and workflows specific to a user's role
- **Notes and bookmarks** — persistent context the agent can reference
- **Integrations** — connect to additional services not covered by the standard servers

### How It Works

```
User registers a plugin
        │
        ▼
Personal MCP Server stores plugin definition
        │
        ▼
When user messages the Personal agent:
  1. Agent Orchestrator calls Personal MCP Server
  2. Server includes user's custom tools in tool definitions
  3. Azure OpenAI can select user's custom tools during reasoning
  4. Server executes the custom tool and returns results
```

### Plugin Structure

Plugins follow the same MCP tool pattern:

- **Name** — unique identifier for the tool
- **Description** — natural language description for the AI model
- **Input Schema** — JSON Schema defining expected arguments
- **Handler** — the execution logic

This makes the Personal agent infinitely extensible without modifying the core platform.
