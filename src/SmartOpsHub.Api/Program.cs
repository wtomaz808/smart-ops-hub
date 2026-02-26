using SmartOpsHub.Api.Endpoints;
using SmartOpsHub.Api.Orchestration;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Infrastructure.AI;
using SmartOpsHub.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Authentication (Microsoft Entra ID / Azure AD) ---
var entraIdSection = builder.Configuration.GetSection("AzureAd");
if (entraIdSection.Exists())
{
    // Microsoft.Identity.Web integrates with ASP.NET Core auth
    builder.Services.AddAuthentication()
        .AddJwtBearer("Bearer", options =>
        {
            options.Authority = entraIdSection["Instance"] + entraIdSection["TenantId"];
            options.Audience = entraIdSection["ClientId"];
            options.TokenValidationParameters.ValidIssuer =
                entraIdSection["Instance"] + entraIdSection["TenantId"] + "/v2.0";
        });
    builder.Services.AddAuthorization();
}

// --- SignalR ---
builder.Services.AddSignalR();

// --- Swagger / OpenAPI ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SmartOpsHub API",
        Version = "v1",
        Description = "Multi-agent operations hub powered by AI and MCP."
    });
});

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000", "http://localhost:5173"];
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- Application Services ---
builder.Services.AddSingleton<IAgentRegistry, AgentRegistryService>();
builder.Services.AddSingleton<IAgentOrchestrator, AgentOrchestrator>();
builder.Services.AddSingleton<IAiCompletionService, AzureOpenAiCompletionService>();
builder.Services.AddSingleton<McpToolExecutor>();

// IMcpGateway — registered from Infrastructure if available, otherwise a stub
builder.Services.AddSingleton<IMcpGateway>(sp =>
{
    // Resolve from DI if Infrastructure registers one; placeholder for startup
    var gateway = sp.GetService<IMcpGateway>();
    return gateway ?? throw new InvalidOperationException(
        "IMcpGateway is not registered. Register an implementation in the Infrastructure layer.");
});

// --- Health Checks ---
builder.Services.AddHealthChecks();

var app = builder.Build();

// --- Middleware pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartOpsHub API v1"));
}

app.UseCors();

if (entraIdSection.Exists())
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// --- Map endpoints ---
app.MapHealthEndpoints();
app.MapAgentEndpoints();
app.MapHealthChecks("/healthz");

app.Run();

// Make the implicit Program class accessible to integration tests
public partial class Program { }
