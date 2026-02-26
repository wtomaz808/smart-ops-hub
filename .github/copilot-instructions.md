# Smart Ops Hub — Copilot Instructions

## Project Overview
Smart Ops Hub is a cloud-native, AI-enabled operations platform deployed to Azure Government.
It features a multi-agent chat workspace where users interact with specialized AI agents
(GitHub, Azure DevOps, Azure Ops, .NET Dev, Personal, etc.) simultaneously via side-by-side chat panels.

## Architecture
- **Runtime**: .NET 9
- **Frontend**: Blazor Server with interactive Server-side rendering
- **Backend**: ASP.NET Core Minimal API with SignalR AgentHub
- **Database**: Azure SQL Database (EF Core, managed identity auth)
- **AI**: Azure OpenAI + Azure AI Services
- **Auth**: Microsoft Entra ID (MSAL)
- **Hosting**: Azure Container Apps (Azure Gov)
- **IaC**: Bicep
- **MCP Servers**: 7 containerized MCP servers + gateway in Azure Container Apps

## Key Patterns
- **Managed Identities** for all service-to-service auth (no passwords)
- **Agent Orchestrator** pattern: user message → Azure OpenAI reasoning → MCP tool execution → streaming response
- **SignalR** for real-time agent chat streaming
- **MCP Protocol** (Model Context Protocol) for tool extensibility

## Coding Conventions
- Use Minimal API style (not controllers)
- Use records for DTOs and value objects
- Use interfaces for all service dependencies
- Nullable reference types enabled globally
- TreatWarningsAsErrors enabled
- xUnit for unit tests, bUnit for Blazor component tests
