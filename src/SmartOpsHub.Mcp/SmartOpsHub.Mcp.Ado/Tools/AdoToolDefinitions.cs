using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Mcp.Ado.Tools;

public static class AdoToolDefinitions
{
    public static readonly McpToolDefinition CreateEpic = new()
    {
        Name = "ado_create_epic",
        Description = "Creates a new Epic work item in Azure DevOps",
        InputSchema = """{"type":"object","properties":{"organization":{"type":"string"},"project":{"type":"string"},"title":{"type":"string"},"description":{"type":"string"}},"required":["organization","project","title"]}"""
    };

    public static readonly McpToolDefinition CreateWorkItem = new()
    {
        Name = "ado_create_work_item",
        Description = "Creates a new work item in Azure DevOps",
        InputSchema = """{"type":"object","properties":{"organization":{"type":"string"},"project":{"type":"string"},"type":{"type":"string"},"title":{"type":"string"},"description":{"type":"string"}},"required":["organization","project","type","title"]}"""
    };

    public static readonly McpToolDefinition ListWorkItems = new()
    {
        Name = "ado_list_work_items",
        Description = "Lists work items in an Azure DevOps project",
        InputSchema = """{"type":"object","properties":{"organization":{"type":"string"},"project":{"type":"string"},"type":{"type":"string"},"state":{"type":"string"}},"required":["organization","project"]}"""
    };

    public static readonly McpToolDefinition GetSprint = new()
    {
        Name = "ado_get_sprint",
        Description = "Gets current sprint information",
        InputSchema = """{"type":"object","properties":{"organization":{"type":"string"},"project":{"type":"string"},"team":{"type":"string"}},"required":["organization","project"]}"""
    };

    public static readonly McpToolDefinition UpdateWorkItemState = new()
    {
        Name = "ado_update_work_item_state",
        Description = "Updates the state of an Azure DevOps work item",
        InputSchema = """{"type":"object","properties":{"organization":{"type":"string"},"project":{"type":"string"},"workItemId":{"type":"integer"},"state":{"type":"string"}},"required":["organization","project","workItemId","state"]}"""
    };

    public static readonly McpToolDefinition QueryWorkItems = new()
    {
        Name = "ado_query_work_items",
        Description = "Queries work items using WIQL",
        InputSchema = """{"type":"object","properties":{"organization":{"type":"string"},"project":{"type":"string"},"query":{"type":"string"}},"required":["organization","project","query"]}"""
    };

    public static IReadOnlyList<McpToolDefinition> All =>
    [
        CreateEpic, CreateWorkItem, ListWorkItems, GetSprint, UpdateWorkItemState, QueryWorkItems
    ];
}
