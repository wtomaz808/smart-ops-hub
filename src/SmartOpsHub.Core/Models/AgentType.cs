namespace SmartOpsHub.Core.Models;

/// <summary>
/// Identifies a specific MCP tool server (1:1 with MCP client implementations).
/// </summary>
public enum McpServerType
{
    GitHub,
    Azure,
    AzureDevOps,
    DotNetDev,
    AiLlm,
    DevOps,
    Personal
}
