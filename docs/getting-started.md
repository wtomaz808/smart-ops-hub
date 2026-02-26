# Getting Started

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 9.0.311+ | `global.json` pins the version |
| Docker Desktop | Latest | For local `docker compose` |
| Azure CLI | Latest | `az cloud set --name AzureUSGovernment` for Azure Gov |
| Git | Latest | |
| IDE (recommended) | VS 2022 / VS Code + C# Dev Kit | |

### Azure Government Access

You need an Azure Gov subscription with:

- Azure OpenAI Service provisioned
- Permissions to create Container Apps, SQL Database, and Container Registry

## Local Development Setup

### 1. Clone and Restore

```bash
git clone <repo-url>
cd aiops
dotnet restore
```

### 2. Verify the Build

```bash
dotnet build smart-ops-hub.sln
```

The solution uses `TreatWarningsAsErrors` — all warnings must be resolved before a successful build.

### 3. Configure App Settings

For local development, the API and Web projects read from `appsettings.Development.json`. Key settings:

```jsonc
// src/SmartOpsHub.Api/appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=SmartOpsHub;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true"
  },
  "AzureOpenAI": {
    "Endpoint": "https://<your-aoai>.openai.azure.us/",
    "DeploymentName": "gpt-4o"
  }
}
```

> **Note**: In deployed environments, managed identities replace connection strings and keys.

## Building and Running

### Option A: Docker Compose (Recommended)

The simplest way to run the full stack locally:

```bash
docker compose up --build
```

This starts four services:

| Service | Port | Description |
|---|---|---|
| `web` | 5000 | Blazor Server frontend |
| `api` | 5001 | ASP.NET Core API + SignalR |
| `mcp-gateway` | 5002 | MCP Gateway |
| `sql` | 1433 | SQL Server 2022 |

To stop:

```bash
docker compose down
```

To reset the database volume:

```bash
docker compose down -v
```

### Option B: Run Individual Projects

Run each project in a separate terminal:

```bash
# Terminal 1 — API
cd src/SmartOpsHub.Api
dotnet run

# Terminal 2 — Web
cd src/SmartOpsHub.Web
dotnet run

# Terminal 3 — MCP Gateway
cd src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.Gateway
dotnet run
```

Ensure SQL Server is running (via Docker or a local instance) before starting the API.

## Running Tests

### All Tests

```bash
dotnet test smart-ops-hub.sln
```

### Specific Test Projects

```bash
# Unit tests
dotnet test tests/SmartOpsHub.Core.Tests

# API integration tests
dotnet test tests/SmartOpsHub.Api.Tests

# Blazor component tests (bUnit)
dotnet test tests/SmartOpsHub.Web.Tests

# MCP server tests
dotnet test tests/SmartOpsHub.Mcp.Tests

# End-to-end tests
dotnet test tests/SmartOpsHub.E2E.Tests
```

### With Verbosity

```bash
dotnet test --verbosity normal --logger "console;verbosity=detailed"
```

## Docker Compose Reference

The `docker-compose.yml` at the repo root defines the local development topology:

```yaml
services:
  web:          # Blazor Server → port 5000
  api:          # Minimal API → port 5001, depends on sql + mcp-gateway
  mcp-gateway:  # MCP Gateway → port 5002
  sql:          # SQL Server 2022 → port 1433

volumes:
  sqldata:      # Persistent SQL data
```

### Rebuilding a Single Service

```bash
docker compose up --build api
```

### Viewing Logs

```bash
docker compose logs -f api
docker compose logs -f mcp-gateway
```

## Next Steps

- [Architecture](architecture.md) — understand the system design
- [MCP Servers](mcp-servers.md) — learn about the MCP server layer
- [Multi-Agent Workspace](multi-agent-workspace.md) — explore the chat workspace
- [Copilot CLI Guide](copilot-cli-guide.md) — use GitHub Copilot CLI with this project
