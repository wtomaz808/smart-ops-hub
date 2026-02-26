using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Mcp.DevOps.Tools;

public static class DevOpsToolDefinitions
{
    public static readonly McpToolDefinition ListPipelines = new()
    {
        Name = "devops_list_pipelines",
        Description = "Lists CI/CD pipelines",
        InputSchema = """{"type":"object","properties":{"organization":{"type":"string"},"project":{"type":"string"}},"required":["organization","project"]}"""
    };

    public static readonly McpToolDefinition TriggerPipeline = new()
    {
        Name = "devops_trigger_pipeline",
        Description = "Triggers a CI/CD pipeline run",
        InputSchema = """{"type":"object","properties":{"organization":{"type":"string"},"project":{"type":"string"},"pipelineId":{"type":"integer"},"branch":{"type":"string"}},"required":["organization","project","pipelineId"]}"""
    };

    public static readonly McpToolDefinition GetPipelineStatus = new()
    {
        Name = "devops_get_pipeline_status",
        Description = "Gets the status of a pipeline run",
        InputSchema = """{"type":"object","properties":{"organization":{"type":"string"},"project":{"type":"string"},"runId":{"type":"integer"}},"required":["organization","project","runId"]}"""
    };

    public static readonly McpToolDefinition ListDeployments = new()
    {
        Name = "devops_list_deployments",
        Description = "Lists recent deployments",
        InputSchema = """{"type":"object","properties":{"organization":{"type":"string"},"project":{"type":"string"},"environment":{"type":"string"}},"required":["organization","project"]}"""
    };

    public static readonly McpToolDefinition GetDeploymentLogs = new()
    {
        Name = "devops_get_deployment_logs",
        Description = "Gets logs for a specific deployment",
        InputSchema = """{"type":"object","properties":{"organization":{"type":"string"},"project":{"type":"string"},"deploymentId":{"type":"integer"}},"required":["organization","project","deploymentId"]}"""
    };

    public static IReadOnlyList<McpToolDefinition> All =>
    [
        ListPipelines, TriggerPipeline, GetPipelineStatus, ListDeployments, GetDeploymentLogs
    ];
}
