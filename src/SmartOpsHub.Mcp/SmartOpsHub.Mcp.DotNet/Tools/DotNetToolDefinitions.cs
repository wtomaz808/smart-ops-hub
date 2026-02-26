using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Mcp.DotNet.Tools;

public static class DotNetToolDefinitions
{
    public static readonly McpToolDefinition BuildProject = new()
    {
        Name = "dotnet_build_project",
        Description = "Builds a .NET project or solution",
        InputSchema = """{"type":"object","properties":{"projectPath":{"type":"string"},"configuration":{"type":"string","enum":["Debug","Release"]}},"required":["projectPath"]}"""
    };

    public static readonly McpToolDefinition RunTests = new()
    {
        Name = "dotnet_run_tests",
        Description = "Runs tests in a .NET project",
        InputSchema = """{"type":"object","properties":{"projectPath":{"type":"string"},"filter":{"type":"string"},"configuration":{"type":"string"}},"required":["projectPath"]}"""
    };

    public static readonly McpToolDefinition ListPackages = new()
    {
        Name = "dotnet_list_packages",
        Description = "Lists NuGet packages in a .NET project",
        InputSchema = """{"type":"object","properties":{"projectPath":{"type":"string"},"includeOutdated":{"type":"boolean"}},"required":["projectPath"]}"""
    };

    public static readonly McpToolDefinition AnalyzeCode = new()
    {
        Name = "dotnet_analyze_code",
        Description = "Runs code analysis on a .NET project",
        InputSchema = """{"type":"object","properties":{"projectPath":{"type":"string"},"severity":{"type":"string","enum":["info","warning","error"]}},"required":["projectPath"]}"""
    };

    public static readonly McpToolDefinition AddPackage = new()
    {
        Name = "dotnet_add_package",
        Description = "Adds a NuGet package to a .NET project",
        InputSchema = """{"type":"object","properties":{"projectPath":{"type":"string"},"packageName":{"type":"string"},"version":{"type":"string"}},"required":["projectPath","packageName"]}"""
    };

    public static IReadOnlyList<McpToolDefinition> All =>
    [
        BuildProject, RunTests, ListPackages, AnalyzeCode, AddPackage
    ];
}
