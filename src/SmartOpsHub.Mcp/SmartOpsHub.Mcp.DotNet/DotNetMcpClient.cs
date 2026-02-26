using System.Text.Json;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.DotNet.Tools;

namespace SmartOpsHub.Mcp.DotNet;

public sealed class DotNetMcpClient : IMcpClient
{
    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(DotNetToolDefinitions.All);

    public Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken cancellationToken = default)
    {
        var result = toolCall.ToolName switch
        {
            "dotnet_build_project" => Stub(toolCall, new { success = true, output = "Build succeeded. 0 Warning(s) 0 Error(s)" }),
            "dotnet_run_tests" => Stub(toolCall, new { passed = 10, failed = 0, skipped = 0, total = 10 }),
            "dotnet_list_packages" => Stub(toolCall, new[] { new { name = "Newtonsoft.Json", version = "13.0.3", latest = "13.0.3" } }),
            "dotnet_analyze_code" => Stub(toolCall, new { warnings = 0, errors = 0, suggestions = Array.Empty<object>() }),
            "dotnet_add_package" => Stub(toolCall, new { success = true, package = "PackageName", version = "1.0.0" }),
            _ => new McpToolResult { ToolCallId = toolCall.Id, Content = $"Unknown tool: {toolCall.ToolName}", IsError = true }
        };

        return Task.FromResult(result);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    private static McpToolResult Stub(McpToolCall toolCall, object data)
        => new() { ToolCallId = toolCall.Id, Content = JsonSerializer.Serialize(data) };
}
