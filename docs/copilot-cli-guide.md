# Using GitHub Copilot CLI with Smart Ops Hub

GitHub Copilot CLI is a terminal-native AI assistant that understands your codebase. This guide covers how to use it effectively with the Smart Ops Hub project.

## Installation

Install using one of these methods:

```bash
# Windows (WinGet)
winget install GitHub.Copilot

# macOS / Linux (Homebrew)
brew install copilot-cli

# Cross-platform (npm)
npm install -g @github/copilot
```

## Launching and Authentication

```bash
# Launch from the project root
cd C:\DSOP\projects\aiops
copilot
```

On first launch, use `/login` to authenticate with your GitHub account. Copilot respects the project's custom instructions in `.github/copilot-instructions.md`, which includes architecture context, coding conventions, and tech stack details specific to Smart Ops Hub.

## Key Commands

| Command | Description |
|---|---|
| `/mcp` | Manage MCP server configuration — connect Copilot to external tool servers |
| `/model` | Select the AI model (Claude Sonnet 4.5, GPT-5, etc.) |
| `/diff` | Review all uncommitted changes in the working directory |
| `/review` | Run the code review agent to analyze staged/unstaged changes |
| `/help` | Show all available commands and keyboard shortcuts |
| `/plan` | Create an implementation plan before writing code |
| `/init` | Initialize Copilot instructions for the repository |
| `/context` | Show context window token usage |
| `/compact` | Summarize conversation to reduce context usage |

## Mentioning Files with @

Use `@` to include specific files in your prompt context:

```
@src/SmartOpsHub.Core/Interfaces/IAgentOrchestrator.cs Explain how the orchestrator routes messages to agents

@src/SmartOpsHub.Mcp/SmartOpsHub.Mcp.Gateway/Program.cs Add health check aggregation for all MCP servers

@docker-compose.yml Add a new MCP server container for the Kubernetes agent
```

This is particularly useful for Smart Ops Hub because the project spans many layers (Blazor, API, MCP servers). Mentioning the relevant files gives Copilot precise context.

## Plan Mode (Shift+Tab)

Press **Shift+Tab** to cycle through modes:

- **Interactive mode** (default) — Copilot executes changes directly
- **Plan mode** — Copilot creates a detailed plan before making changes

Use plan mode for complex changes like adding a new MCP server or modifying the Agent Orchestrator flow, so you can review the approach before any files are modified.

## Using MCP Servers

GitHub Copilot CLI supports connecting to MCP servers, extending its capabilities with external tools. Use `/mcp` to manage configurations.

### Connecting to Project MCP Servers

If the Smart Ops Hub MCP Gateway is running locally (e.g., via `docker compose up`), you can configure Copilot to use it:

```bash
/mcp
# Follow prompts to add a server
# Endpoint: http://localhost:5002
```

### GitHub MCP Server

Copilot ships with GitHub's MCP server by default, giving it access to:

- Repository search, file contents, commit history
- Issue and pull request management
- GitHub Actions workflows and logs

This is especially useful for Smart Ops Hub workflows like checking CI status or reviewing PRs.

## Best Practices

### 1. Launch from the Project Root

Always start Copilot from `C:\DSOP\projects\aiops` so it picks up:

- `.github/copilot-instructions.md` (project-specific AI instructions)
- `global.json` (.NET SDK version)
- `smart-ops-hub.sln` (solution structure)

### 2. Use `/review` Before Committing

```
/review
```

The code review agent will analyze your changes and flag bugs, security issues, or logic errors — it ignores style/formatting noise.

### 3. Reference Architecture Files

When asking about design decisions, mention the relevant interfaces:

```
@src/SmartOpsHub.Core/Interfaces/IMcpGateway.cs
@src/SmartOpsHub.Core/Models/McpModels.cs
How does the gateway resolve which MCP server to call?
```

### 4. Use `/diff` to Stay Oriented

After a series of changes, run `/diff` to see everything that's been modified before committing.

### 5. Use `/compact` for Long Sessions

Smart Ops Hub has a large codebase. If your conversation grows long, use `/compact` to summarize history and free up context window space.

## Common Workflows

### Code Review

```bash
copilot
> /review                    # Review all uncommitted changes
> /diff                      # See a summary of what changed
```

### Debugging a Failing Test

```
@tests/SmartOpsHub.Core.Tests/AgentOrchestratorTests.cs
This test is failing with a NullReferenceException on line 42. Help me debug it.
```

### Adding a New MCP Server

```
I need to add a new MCP server called SmartOpsHub.Mcp.Kubernetes for managing K8s clusters.
Walk me through all the files I need to create and modify.
```

In plan mode (**Shift+Tab**), Copilot will outline the steps (new project, Dockerfile, docker-compose entry, AgentType enum update, gateway registration) before making changes.

### Understanding the Architecture

```
@.github/copilot-instructions.md
@src/SmartOpsHub.Core/Interfaces/IAgentOrchestrator.cs
Explain the full request flow from a user typing a message to receiving a response.
```

### Working with Infrastructure

```
@infra/
Help me add a Bicep module for a new Container App that hosts the Kubernetes MCP server.
```

## Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `Shift+Tab` | Cycle modes (interactive → plan) |
| `Ctrl+S` | Run command while preserving input |
| `Ctrl+T` | Toggle model reasoning display |
| `@` | Mention files for context |
| `!` | Execute a shell command directly |
| `Esc` | Cancel current operation |
| `Ctrl+C` | Cancel / clear / exit |
| `↑` `↓` | Navigate command history |
