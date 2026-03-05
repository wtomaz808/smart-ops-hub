# AgentOpsHub — Project Status

> Last updated: 2026-03-04

## Overview

AgentOpsHub is a cloud-native, AI-enabled operations platform deployed to **Azure Government**. It features a multi-agent chat workspace where users interact with specialized AI agent categories via dedicated chat panels, each backed by Azure OpenAI models and MCP (Model Context Protocol) tool servers.

---

## Commit History

| # | Commit | Description |
|---|--------|-------------|
| 1 | `752390b` | Initial project scaffold — .NET 9 solution, Blazor Server frontend, Minimal API backend, 7 MCP server stubs, Docker Compose, CI/CD workflows |
| 2 | `272a426` | Real GitHub MCP client with Octokit API integration |
| 3 | `b082921` | Real API integrations for all remaining MCP clients (Azure, ADO, .NET Dev, AI/LLM, DevOps, Personal) |
| 4 | `2867c21` | Comprehensive MCP test suite — 81 tests across all servers |
| 5 | `88874ce` | Expanded test coverage across all layers — 130 tests total |
| 6 | `950bf01` | Session persistence with EF Core repositories |
| 7 | `0c59c94` | Docker build validation and MCP Gateway added to CI/CD pipeline |
| 8 | `9219ecb` | Infrastructure deployment workflow, script, and guide |
| 9 | `189e598` | Streaming responses with typewriter effect via SignalR |
| 10 | `b7a820e` | Split infra workflow — Bicep compile runs without Azure creds |
| 11 | `eafd778` | Fix port mismatches, CORS, SignalR URL, and auth guard for local dev |
| 12 | `e7ebbd2` | Workspace as home page, admin page, remove demo pages |
| 13 | `a12e2d2` | Add `start-local.ps1` script, update README quick-start |
| 14 | `7b002cf` | Containerize local dev — fix Dockerfiles, docker-compose, and start-local.ps1 |
| 15 | `ccd599d` | Consolidate 7 agents into 4 category-based interfaces with sidebar nav |
| 16 | `bf0ce6e` | Add file upload to chat panels, enable BizOps and Training agents |
| 17 | `44e15a4` | Rebrand to AgentOpsHub, move agent nav to main sidebar, add agent creation |
| 18 | `18c1431` | Add landing page, Help agent, and Home nav link |
| 19 | `a7a1a45` | Dual model selector (GPT-4.1 + GPT-4o), region to usgovarizona |
| 20 | `a75f7b6` | Add Entra ID app registration to deployment pipeline |
| 21 | `a6ff3a5` | Rename resource group to `rg-AgentOpsHub-{env}` |
| 22 | `88d0c6f` | Azure Gov deployment fixes — RAI policy, workload profiles, log analytics |
| 23 | `7dc62ec` | Add health endpoints and fix HTTPS redirect for Container Apps |
| 24 | `ce09531` | Rename Azure resources to `aoh-{type}-{env}` convention |
| 25 | `4f81d7a` | deploy.ps1 JMESPath query fix for Windows compatibility |
| 26 | `11b8cf5` | Move SignalR connect to `OnAfterRenderAsync` to prevent SSR timeout |

---

## Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                     Azure Government                         │
│                      (usgovarizona)                           │
│                                                              │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────────┐    │
│  │ aoh-ca-web  │  │ aoh-ca-api   │  │   aoh-ca-gw      │    │
│  │ Blazor UI   │→ │ Minimal API  │→ │  MCP Gateway      │    │
│  │ SignalR     │  │ AgentHub     │  │  Tool Router      │    │
│  │ :8080       │  │ :8080        │  │  :8080            │    │
│  └─────────────┘  └──────────────┘  └──────────────────┘    │
│         ↓                ↓                                   │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────────┐    │
│  │ Entra ID    │  │ Azure OpenAI │  │ Azure SQL        │    │
│  │ (MSAL)      │  │ GPT-4.1      │  │ aoh-sql-dev      │    │
│  │             │  │ GPT-4o       │  │                   │    │
│  └─────────────┘  └──────────────┘  └──────────────────┘    │
│                                                              │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────────┐    │
│  │ Key Vault   │  │ ACR          │  │ App Insights     │    │
│  │ aohkvdev    │  │ aohacrdev    │  │ aoh-appi-dev     │    │
│  └─────────────┘  └──────────────┘  └──────────────────┘    │
└──────────────────────────────────────────────────────────────┘
```

### Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 9 |
| Frontend | Blazor Server (Interactive Server rendering) |
| Backend | ASP.NET Core Minimal API + SignalR AgentHub |
| Database | Azure SQL Database (EF Core, managed identity) |
| AI | Azure OpenAI (GPT-4.1 + GPT-4o) |
| Auth | Microsoft Entra ID (MSAL) |
| Hosting | Azure Container Apps (Azure Gov, usgovarizona) |
| IaC | Bicep (9 modules) |
| CI/CD | GitHub Actions (ci.yml, cd.yml, infra.yml) |
| Containers | Docker Compose (local), ACR (deployed) |

---

## Agent Categories

The platform organizes AI agents into 5 category-based interfaces, each with its own chat panel, model selector, and file upload capability:

| Category | Icon | MCP Servers | Purpose |
|----------|------|-------------|---------|
| **DevOps** | ⚙️ | GitHub, Azure, AzureDevOps, DevOps, DotNetDev | Repos, PRs, CI/CD, infrastructure, .NET dev |
| **BizOps** | 💼 | *(coming soon)* | Corporate comms, meetings, business workflows |
| **Training & Research** | 📚 | AI/LLM | Documentation, learning, prompt engineering |
| **Personal** | 🎯 | Personal | Productivity, calendar, personal tools |
| **Help** | ❓ | GitHub | Platform docs, usage guides, technical support |

Each agent supports:
- **Dual model selection**: GPT-4.1 (2025-04-14) and GPT-4o (2024-11-20) via dropdown
- **File upload**: Up to 5 files, 5 MB each, 25+ text types + images/PDFs
- **Streaming responses**: Real-time typewriter effect via SignalR
- **Session management**: Per-agent SignalR connections with auto-reconnect

---

## Azure Resources (dev environment)

All resources follow the `aoh-{type}-{env}` naming convention:

| Resource | Name | Purpose |
|----------|------|---------|
| Resource Group | `rg-AgentOpsHub-dev` | Container for all resources |
| Managed Identity | `aoh-id-dev` | Service-to-service auth (no passwords) |
| Virtual Network | `aoh-vnet-dev` | Network isolation |
| Log Analytics | `aoh-log-dev` | Centralized logging |
| App Insights | `aoh-appi-dev` | Application telemetry |
| Container Registry | `aohacrdev` | Docker image storage |
| Key Vault | `aohkvdev` | Secrets management |
| SQL Server | `aoh-sql-dev` | Database server |
| SQL Database | `aoh-db-dev` | Application database |
| OpenAI | `aoh-oai-dev` | GPT-4.1 + GPT-4o deployments |
| AI Services | `aoh-ais-dev` | Azure AI Services |
| Container Apps Env | `aoh-cae-dev` | Container Apps managed environment |
| Web App | `aoh-ca-web-dev` | Blazor frontend |
| API App | `aoh-ca-api-dev` | Backend API + SignalR hub |
| MCP Gateway | `aoh-ca-gw-dev` | MCP tool routing |

### Live URLs

- **Web**: `https://aoh-ca-web-dev.politepond-89f95a03.usgovarizona.azurecontainerapps.us`
- **API**: `https://aoh-ca-api-dev.politepond-89f95a03.usgovarizona.azurecontainerapps.us`
- **Gateway**: `https://aoh-ca-gw-dev.politepond-89f95a03.usgovarizona.azurecontainerapps.us`

### Key Configuration

| Setting | Value |
|---------|-------|
| Tenant ID | `d14ab12e-c535-4865-a593-c4115e7de102` |
| Subscription | `df79eff1-4ca3-4d21-9c6b-64dd15c253e8` (Sub_Tomasiewicz_DEV) |
| Region | `usgovarizona` |
| Entra Instance | `https://login.microsoftonline.us/` |
| App Registration | `a2cb5861-04cd-4bb3-a901-f7d6ee216839` (AgentOpsHub-dev) |

---

## Test Coverage

- **147 tests passing**, 0 warnings
- Test framework: xUnit + bUnit
- No mocking library — inline fakes, `WebApplicationFactory`, bUnit `TestContext`
- `TreatWarningsAsErrors` enabled globally; test projects use `<NoWarn>CA1707</NoWarn>` for underscore method names

```
dotnet test smart-ops-hub.sln
```

---

## Local Development

```powershell
# Docker mode (recommended)
.\start-local.ps1 -Docker

# .NET process mode
.\start-local.ps1

# Rebuild containers
.\start-local.ps1 -Docker -Build

# Stop containers
.\start-local.ps1 -Down
```

| Service | Local Port |
|---------|-----------|
| Web (Blazor) | `http://localhost:5050` |
| API | `http://localhost:5100` |
| MCP Gateway | `http://localhost:5200` |
| SQL Server | `localhost:1433` |

---

## Deployment

```powershell
# Deploy dev environment
.\infra\scripts\deploy.ps1 -Environment dev

# Preview changes
.\infra\scripts\deploy.ps1 -Environment dev -WhatIf

# Skip app registration (already created)
.\infra\scripts\deploy.ps1 -Environment dev -SkipAppReg
```

After Bicep deploys, push Docker images:
```powershell
az acr login --name aohacrdev
docker compose build
docker tag aiops-web:latest aohacrdev.azurecr.us/aoh-web:latest
docker tag aiops-api:latest aohacrdev.azurecr.us/aoh-api:latest
docker tag aiops-mcp-gateway:latest aohacrdev.azurecr.us/aoh-gw:latest
docker push aohacrdev.azurecr.us/aoh-web:latest
docker push aohacrdev.azurecr.us/aoh-api:latest
docker push aohacrdev.azurecr.us/aoh-gw:latest
```

---

## Azure Gov Deployment Lessons Learned

These issues were discovered through iterative deployment and may help future troubleshooting:

1. **RAI Policy**: `raiPolicyName: 'Microsoft.DefaultV2'` does **not** exist in Azure Gov — omit it entirely; Azure applies the default policy automatically.

2. **Workload Profiles**: Container Apps in Azure Gov **require** workload profiles — add `workloadProfiles: [{ name: 'Consumption', workloadProfileType: 'Consumption' }]` to the managed environment.

3. **Log Analytics Shared Key**: Container Apps environment provisioning requires the `sharedKey` (not just `customerId`) — output via `logAnalytics.listKeys().primarySharedKey`.

4. **TLS Termination**: Container Apps terminate TLS at the ingress (443 → 8080 internal). Apps must **not** use `UseHttpsRedirection()` in production — it causes "Failed to determine the https port for redirect" errors and liveness probe failures.

5. **Health Endpoints**: Container Apps need `/healthz` (liveness) and `/ready` (readiness) endpoints, otherwise the platform restarts containers.

6. **SSR Prerendering**: Blazor `@rendermode InteractiveServer` pages prerender during SSR. Any async work in `OnInitializedAsync` (like SignalR connections) will block the HTTP response. Move network calls to `OnAfterRenderAsync(firstRender: true)`.

7. **Container App URLs**: The format `{app-name}.{random-hash}.{region}.azurecontainerapps.us` is not customizable. Custom domains require CNAME configuration.

---

## Current Status

### ✅ What's Working
- Landing page with hero section, use case cards, feature overview
- 5 agent category chat interfaces with sidebar navigation
- Dual model selector (GPT-4.1 / GPT-4o) per agent
- File upload (5 MB, 25+ text types)
- Streaming responses via SignalR
- Admin settings page
- Add Agent creation page
- All 3 Container Apps running in Azure Gov
- Entra ID app registration configured
- 147 tests passing, 0 warnings
- CI/CD workflows (ci.yml, infra.yml, cd.yml)
- Local Docker development with `start-local.ps1`

### 🔄 In Progress / Next Steps
- **Azure OpenAI connectivity**: Models are deployed but agents haven't been tested end-to-end with live Azure OpenAI responses yet
- **EF Core database migrations**: SQL schema exists in code but hasn't been applied to the deployed Azure SQL instance
- **Entra ID auth activation**: App registration exists; MSAL middleware is conditional and needs `AzureAd` config section populated with the client secret
- **MCP server tool execution**: MCP clients have real API integrations but need Azure credentials and API keys configured
- **Custom domain**: Container App URLs use auto-generated domain; custom domain needs CNAME setup
- **GitHub Actions variables**: ACR_NAME, container app names, and other variables need to be set in repository settings for CI/CD to deploy automatically
- **Documentation branding update**: ~~Most docs still reference "Smart Ops Hub"~~ — completed global rename to "AgentOpsHub"

---

## Project Structure

```
smart-ops-hub.sln
├── src/
│   ├── SmartOpsHub.Core/          # Domain models, interfaces, enums
│   ├── SmartOpsHub.Infrastructure/ # EF Core, AI services, MCP clients, agent registry
│   ├── SmartOpsHub.Api/           # Minimal API, SignalR AgentHub, orchestration
│   ├── SmartOpsHub.Web/           # Blazor Server UI, chat panels, layouts
│   └── SmartOpsHub.Mcp/           # 7 MCP server projects + gateway
├── tests/
│   ├── SmartOpsHub.Api.Tests/
│   ├── SmartOpsHub.Core.Tests/
│   ├── SmartOpsHub.Infrastructure.Tests/
│   ├── SmartOpsHub.Mcp.Tests/
│   └── SmartOpsHub.Web.Tests/
├── infra/
│   ├── main.bicep                 # Central Bicep orchestrator (9 modules)
│   ├── modules/                   # Individual Bicep modules
│   ├── parameters/                # dev.bicepparam, prod.bicepparam
│   └── scripts/                   # deploy.ps1, setup-app-registration.ps1
├── .github/workflows/             # ci.yml, cd.yml, infra.yml
├── docs/                          # Architecture, deployment, getting started guides
├── docker-compose.yml             # Local dev: web, api, gateway, sql
└── start-local.ps1                # Local dev launcher (Docker or .NET process)
```
