using Azure.Identity;
using Azure.Monitor.Query;
using Azure.ResourceManager;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;
using SmartOpsHub.Mcp.Ado;
using SmartOpsHub.Mcp.AI;
using SmartOpsHub.Mcp.Azure;
using SmartOpsHub.Mcp.DevOps;
using SmartOpsHub.Mcp.DotNet;
using SmartOpsHub.Mcp.Gateway.Services;
using SmartOpsHub.Mcp.GitHub;
using SmartOpsHub.Mcp.Personal;
using SmartOpsHub.Mcp.Personal.Plugins;

var builder = WebApplication.CreateBuilder(args);

// Shared Azure credential for all Azure SDK clients
var azureCredential = new DefaultAzureCredential();

// Register GitHub API client with auth
builder.Services.AddSingleton<Octokit.GitHubClient>(sp =>
{
    var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("SmartOpsHub"));
    var token = builder.Configuration["GitHub:Token"];
    if (!string.IsNullOrEmpty(token))
        client.Credentials = new Octokit.Credentials(token);
    return client;
});

// Register Azure SDK clients
builder.Services.AddSingleton(new ArmClient(azureCredential));
builder.Services.AddSingleton(new LogsQueryClient(azureCredential));
builder.Services.AddSingleton(new MetricsQueryClient(azureCredential));

// Register Azure OpenAI client for AI MCP
var aoaiEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
if (!string.IsNullOrEmpty(aoaiEndpoint))
    builder.Services.AddSingleton(new Azure.AI.OpenAI.AzureOpenAIClient(new Uri(aoaiEndpoint), azureCredential));
else
    builder.Services.AddSingleton(new Azure.AI.OpenAI.AzureOpenAIClient(new Uri("https://placeholder.openai.azure.us/"), azureCredential));

// Register MCP clients
builder.Services.AddSingleton<GitHubMcpClient>();
builder.Services.AddSingleton<AzureMcpClient>();
builder.Services.AddHttpClient<AdoMcpClient>();
builder.Services.AddSingleton<DotNetMcpClient>();
builder.Services.AddSingleton<AiMcpClient>();
builder.Services.AddHttpClient<DevOpsMcpClient>();

// Register Personal plugins and client
builder.Services.AddSingleton<IPersonalPlugin, FantasyFootballPlugin>();
builder.Services.AddSingleton<IPersonalPlugin, CalendarPlugin>();
builder.Services.AddSingleton<PersonalMcpClient>();

// Register the gateway service
builder.Services.AddSingleton<IMcpGateway>(sp =>
{
    var clients = new List<KeyValuePair<AgentType, IMcpClient>>
    {
        new(AgentType.GitHub, sp.GetRequiredService<GitHubMcpClient>()),
        new(AgentType.Azure, sp.GetRequiredService<AzureMcpClient>()),
        new(AgentType.AzureDevOps, sp.GetRequiredService<AdoMcpClient>()),
        new(AgentType.DotNetDev, sp.GetRequiredService<DotNetMcpClient>()),
        new(AgentType.AiLlm, sp.GetRequiredService<AiMcpClient>()),
        new(AgentType.DevOps, sp.GetRequiredService<DevOpsMcpClient>()),
        new(AgentType.Personal, sp.GetRequiredService<PersonalMcpClient>()),
    };
    return new McpGatewayService(clients);
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "SmartOpsHub.Mcp.Gateway" }));

app.MapGet("/gateway/health", async (IMcpGateway gateway, CancellationToken ct) =>
{
    var status = await gateway.GetHealthStatusAsync(ct);
    var allHealthy = status.Values.All(v => v);
    return Results.Ok(new { Status = allHealthy ? "Healthy" : "Degraded", Services = status });
});

app.Run();
