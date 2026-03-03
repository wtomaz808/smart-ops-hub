using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Infrastructure.Data;

namespace SmartOpsHub.Api.Tests;

internal sealed class TestMcpGateway : IMcpGateway
{
    public Task<IMcpClient> GetClientAsync(McpServerType serverType, CancellationToken ct = default)
        => Task.FromResult<IMcpClient>(new TestMcpClient());

    public Task<IReadOnlyDictionary<McpServerType, bool>> GetHealthStatusAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<McpServerType, bool>>(new Dictionary<McpServerType, bool>());
}

internal sealed class TestMcpClient : IMcpClient
{
    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<McpToolDefinition>>([]);

    public Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken ct = default)
        => Task.FromResult(new McpToolResult { ToolCallId = toolCall.Id, Content = "test" });

    public Task<bool> IsHealthyAsync(CancellationToken ct = default) => Task.FromResult(true);
}

internal sealed class TestAiCompletionService : IAiCompletionService
{
    public Task<string> GetCompletionAsync(IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<McpToolDefinition>? availableTools = null, string? deploymentName = null, CancellationToken ct = default)
        => Task.FromResult("Test AI response");

    public async IAsyncEnumerable<string> StreamCompletionAsync(IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<McpToolDefinition>? availableTools = null, string? deploymentName = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return "Test ";
        yield return "response";
        await Task.CompletedTask;
    }
}

public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace DbContext with InMemory
            var dbDesc = services.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<SmartOpsHubDbContext>));
            if (dbDesc is not null) services.Remove(dbDesc);
            services.AddDbContext<SmartOpsHubDbContext>(options =>
                options.UseInMemoryDatabase("SmartOpsHub_Api_" + Guid.NewGuid().ToString("N")));

            // Replace external services with test fakes
            var mcpDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IMcpGateway));
            if (mcpDesc is not null) services.Remove(mcpDesc);
            services.AddSingleton<IMcpGateway>(new TestMcpGateway());

            var aiDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IAiCompletionService));
            if (aiDesc is not null) services.Remove(aiDesc);
            services.AddSingleton<IAiCompletionService>(new TestAiCompletionService());
        });
    }
}
