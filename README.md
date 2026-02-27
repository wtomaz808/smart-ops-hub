# Smart Ops Hub

> AI-enabled operations platform for Azure Government — a multi-agent chat workspace where DevOps teams collaborate with specialized AI agents in real time.

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4)](https://learn.microsoft.com/aspnet/core/blazor/)
[![Azure Gov](https://img.shields.io/badge/Azure-Government-0078D4)](https://azure.microsoft.com/explore/global-infrastructure/government)

## Overview

Smart Ops Hub is a cloud-native operations platform deployed to **Azure Government**. It brings together AI agents, MCP (Model Context Protocol) servers, and a Blazor Server frontend into a unified command center for infrastructure operations, DevOps workflows, and AI-assisted development.

### Multi-Agent Chat Workspace

The flagship feature is a **multi-agent chat workspace** — a command-center UI with side-by-side chat panels, each connected to a specialized AI agent (GitHub, Azure DevOps, Azure Ops, .NET Dev, AI/LLM, DevOps, Personal). Users send a single message and receive coordinated responses from multiple agents simultaneously, streamed in real time via SignalR.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Blazor Server (Web)                   │
│              Side-by-side Agent Chat Panels              │
└──────────────────────┬──────────────────────────────────┘
                       │ SignalR (AgentHub)
┌──────────────────────▼──────────────────────────────────┐
│                   ASP.NET Core API                       │
│              Agent Orchestrator + Azure OpenAI           │
└──────────────────────┬──────────────────────────────────┘
                       │ HTTP / MCP Protocol
┌──────────────────────▼──────────────────────────────────┐
│                    MCP Gateway                          │
│         Routes requests to 7 MCP servers                │
├─────────┬─────────┬────────┬────────┬────────┬──────────┤
│ GitHub  │  Azure  │  ADO   │ .NET   │  AI    │ DevOps   │
│ Server  │  Server │ Server │ Server │ Server │ Server   │
├─────────┴─────────┴────────┴────────┴────────┴──────────┤
│                   Personal MCP Server                   │
└─────────────────────────────────────────────────────────┘
          All deployed as Azure Container Apps
```

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 9 |
| Frontend | Blazor Server (interactive SSR) |
| Backend API | ASP.NET Core Minimal API + SignalR |
| AI | Azure OpenAI + Azure AI Services |
| Database | Azure SQL Database (EF Core, managed identity) |
| Auth | Microsoft Entra ID (MSAL) |
| Hosting | Azure Container Apps (Azure Government) |
| IaC | Bicep |
| MCP | 7 containerized MCP servers + gateway |
| Testing | xUnit, bUnit |

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (9.0.311+)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for local development)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) with Azure Government cloud configured
- An Azure Government subscription with Azure OpenAI access

## Quick Start

### Option 1 — Startup script (recommended)

```powershell
# Build and start both servers in separate windows
.\start-local.ps1

# Or skip the build if you already compiled
.\start-local.ps1 -NoBuild
```

This opens two PowerShell windows (API + Web). Close them to stop the servers.

### Option 2 — Manual (two terminals)

**Terminal 1 — API:**
```powershell
cd src\SmartOpsHub.Api
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --urls http://localhost:5100
```

**Terminal 2 — Web:**
```powershell
cd src\SmartOpsHub.Web
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --urls http://localhost:5050
```

### Option 3 — Docker Compose

```bash
docker compose up --build
```

### Local URLs

| Service | URL |
|---|---|
| Web (Workspace) | http://localhost:5050 |
| Admin | http://localhost:5050/admin |
| API | http://localhost:5100 |
| API Health | http://localhost:5100/health |

### Run tests

```bash
dotnet test
```

For detailed setup instructions see [docs/getting-started.md](docs/getting-started.md).

## Project Structure

```
smart-ops-hub.sln
├── src/
│   ├── SmartOpsHub.Web/            # Blazor Server frontend
│   ├── SmartOpsHub.Api/            # ASP.NET Core Minimal API + SignalR
│   ├── SmartOpsHub.Core/           # Domain models, interfaces, services
│   ├── SmartOpsHub.Infrastructure/ # EF Core, external integrations
│   └── SmartOpsHub.Mcp/
│       ├── SmartOpsHub.Mcp.Gateway/   # MCP gateway router
│       ├── SmartOpsHub.Mcp.GitHub/    # GitHub operations
│       ├── SmartOpsHub.Mcp.Azure/     # Azure resource operations
│       ├── SmartOpsHub.Mcp.Ado/       # Azure DevOps operations
│       ├── SmartOpsHub.Mcp.DotNet/    # .NET tooling
│       ├── SmartOpsHub.Mcp.AI/        # AI/LLM utilities
│       ├── SmartOpsHub.Mcp.DevOps/    # CI/CD & pipeline tools
│       └── SmartOpsHub.Mcp.Personal/  # Per-user agent plugins
├── tests/
│   ├── SmartOpsHub.Web.Tests/      # bUnit component tests
│   ├── SmartOpsHub.Api.Tests/      # API integration tests
│   ├── SmartOpsHub.Core.Tests/     # Domain unit tests
│   ├── SmartOpsHub.Mcp.Tests/      # MCP server tests
│   └── SmartOpsHub.E2E.Tests/      # End-to-end tests
├── infra/                          # Bicep IaC modules
├── docker-compose.yml
└── docs/                           # Documentation
```

## Documentation

- [Architecture](docs/architecture.md) — system design, data flow, deployment topology
- [Getting Started](docs/getting-started.md) — prerequisites, local setup, running tests
- [MCP Servers](docs/mcp-servers.md) — Model Context Protocol server details
- [Multi-Agent Workspace](docs/multi-agent-workspace.md) — chat workspace design
- [Copilot CLI Guide](docs/copilot-cli-guide.md) — using GitHub Copilot CLI with this project

## License

[MIT](LICENSE)
