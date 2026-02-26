using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Infrastructure.Services;
using SmartOpsHub.Web.Components;
using SmartOpsHub.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Authentication (placeholder config – configure AzureAd section in appsettings when ready)
var azureAdSection = builder.Configuration.GetSection("AzureAd");
if (azureAdSection.Exists() && !string.IsNullOrEmpty(azureAdSection["ClientId"]))
{
    builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration);
    builder.Services.AddControllersWithViews()
        .AddMicrosoftIdentityUI();
}

builder.Services.AddAuthorization();

// Core services
builder.Services.AddSingleton<IAgentRegistry, AgentRegistryService>();
builder.Services.AddScoped<AgentChatService>();
builder.Services.AddSingleton<AgentSessionManager>();

// HttpClient for API communication
builder.Services.AddHttpClient<AgentSessionManager>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue("ApiBaseUrl", "https://localhost:5001/")!);
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
