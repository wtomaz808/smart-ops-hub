using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.DotNet.Tools;

namespace SmartOpsHub.Mcp.DotNet;

public sealed partial class DotNetMcpClient(ILogger<DotNetMcpClient> logger) : IMcpClient
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(DotNetToolDefinitions.All);

    public async Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken cancellationToken = default)
    {
        try
        {
            return toolCall.ToolName switch
            {
                "dotnet_build_project" => await HandleBuildAsync(toolCall, cancellationToken),
                "dotnet_run_tests" => await HandleRunTestsAsync(toolCall, cancellationToken),
                "dotnet_list_packages" => await HandleListPackagesAsync(toolCall, cancellationToken),
                "dotnet_analyze_code" => await HandleAnalyzeAsync(toolCall, cancellationToken),
                "dotnet_add_package" => await HandleAddPackageAsync(toolCall, cancellationToken),
                _ => new McpToolResult { ToolCallId = toolCall.Id, Content = $"Unknown tool: {toolCall.ToolName}", IsError = true }
            };
        }
        catch (Exception ex)
        {
            LogToolError(ex, toolCall.ToolName);
            return new McpToolResult { ToolCallId = toolCall.Id, Content = $".NET CLI error: {ex.Message}", IsError = true };
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var (exitCode, _, _) = await RunDotNetAsync("--version", ct: cancellationToken).ConfigureAwait(false);
            return exitCode == 0;
        }
        catch (Exception ex)
        {
            LogHealthCheckFailed(ex);
            return false;
        }
    }

    private async Task<McpToolResult> HandleBuildAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var projectPath = args.GetProperty("projectPath").GetString()!;
        var config = args.TryGetProperty("configuration", out var c) && c.ValueKind == JsonValueKind.String
            ? c.GetString()! : "Debug";

        var (exitCode, stdout, stderr) = await RunDotNetAsync($"build \"{projectPath}\" --configuration {config}", projectPath, ct).ConfigureAwait(false);

        LogToolExecuted("dotnet_build_project", projectPath);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            IsError = exitCode != 0,
            Content = JsonSerializer.Serialize(new { Success = exitCode == 0, ExitCode = exitCode, Output = stdout, Errors = stderr }, s_jsonOptions)
        };
    }

    private async Task<McpToolResult> HandleRunTestsAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var projectPath = args.GetProperty("projectPath").GetString()!;
        var cliArgs = $"test \"{projectPath}\"";
        if (args.TryGetProperty("filter", out var f) && f.ValueKind == JsonValueKind.String)
            cliArgs += $" --filter \"{f.GetString()}\"";
        if (args.TryGetProperty("configuration", out var c) && c.ValueKind == JsonValueKind.String)
            cliArgs += $" --configuration {c.GetString()}";

        var (exitCode, stdout, stderr) = await RunDotNetAsync(cliArgs, projectPath, ct).ConfigureAwait(false);

        LogToolExecuted("dotnet_run_tests", projectPath);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            IsError = exitCode != 0,
            Content = JsonSerializer.Serialize(new { Success = exitCode == 0, ExitCode = exitCode, Output = stdout, Errors = stderr }, s_jsonOptions)
        };
    }

    private async Task<McpToolResult> HandleListPackagesAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var projectPath = args.GetProperty("projectPath").GetString()!;
        var cliArgs = $"list \"{projectPath}\" package";
        if (args.TryGetProperty("includeOutdated", out var o) && o.GetBoolean())
            cliArgs += " --outdated";

        var (exitCode, stdout, stderr) = await RunDotNetAsync(cliArgs, projectPath, ct).ConfigureAwait(false);

        LogToolExecuted("dotnet_list_packages", projectPath);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            IsError = exitCode != 0,
            Content = JsonSerializer.Serialize(new { Success = exitCode == 0, Output = stdout, Errors = stderr }, s_jsonOptions)
        };
    }

    private async Task<McpToolResult> HandleAnalyzeAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var projectPath = args.GetProperty("projectPath").GetString()!;
        var cliArgs = $"build \"{projectPath}\" --no-restore";
        if (args.TryGetProperty("severity", out var s) && s.ValueKind == JsonValueKind.String)
            cliArgs += $" /p:AnalysisLevel=latest /p:WarningLevel=9999";

        var (exitCode, stdout, stderr) = await RunDotNetAsync(cliArgs, projectPath, ct).ConfigureAwait(false);

        LogToolExecuted("dotnet_analyze_code", projectPath);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            IsError = exitCode != 0,
            Content = JsonSerializer.Serialize(new { Success = exitCode == 0, Output = stdout, Errors = stderr }, s_jsonOptions)
        };
    }

    private async Task<McpToolResult> HandleAddPackageAsync(McpToolCall toolCall, CancellationToken ct)
    {
        var args = JsonDocument.Parse(toolCall.Arguments).RootElement;
        var projectPath = args.GetProperty("projectPath").GetString()!;
        var packageName = args.GetProperty("packageName").GetString()!;
        var cliArgs = $"add \"{projectPath}\" package {packageName}";
        if (args.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.String)
            cliArgs += $" --version {v.GetString()}";

        var (exitCode, stdout, stderr) = await RunDotNetAsync(cliArgs, projectPath, ct).ConfigureAwait(false);

        LogToolExecuted("dotnet_add_package", projectPath);
        return new McpToolResult
        {
            ToolCallId = toolCall.Id,
            IsError = exitCode != 0,
            Content = JsonSerializer.Serialize(new { Success = exitCode == 0, Output = stdout, Errors = stderr }, s_jsonOptions)
        };
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunDotNetAsync(
        string arguments, string? workingDirectory = null, CancellationToken ct = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        if (!string.IsNullOrEmpty(workingDirectory) && Directory.Exists(workingDirectory))
            process.StartInfo.WorkingDirectory = workingDirectory;

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);

        return (process.ExitCode, stdout, stderr);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Executed {ToolName} for {ProjectPath}")]
    private partial void LogToolExecuted(string toolName, string projectPath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error executing .NET tool {ToolName}")]
    private partial void LogToolError(Exception ex, string toolName);

    [LoggerMessage(Level = LogLevel.Warning, Message = ".NET CLI health check failed")]
    private partial void LogHealthCheckFailed(Exception ex);
}
