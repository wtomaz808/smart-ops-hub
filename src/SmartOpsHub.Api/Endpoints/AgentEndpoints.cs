using SmartOpsHub.Api.Hubs;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Api.Endpoints;

public static class AgentEndpoints
{
    public static WebApplication MapAgentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Agents");

        group.MapGet("/agents", (IAgentRegistry registry) =>
        {
            var agents = registry.GetAllAgents();
            return Results.Ok(agents);
        })
        .WithName("ListAgents")
        .WithSummary("List all available agents");

        group.MapGet("/agents/{type}/health", async (AgentType type, IMcpGateway mcpGateway, CancellationToken ct) =>
        {
            try
            {
                var client = await mcpGateway.GetClientAsync(type, ct);
                var healthy = await client.IsHealthyAsync(ct);
                return Results.Ok(new { AgentType = type.ToString(), Healthy = healthy });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { AgentType = type.ToString(), Healthy = false, Error = ex.Message });
            }
        })
        .WithName("CheckAgentHealth")
        .WithSummary("Check MCP server health for an agent type");

        group.MapPost("/sessions", async (CreateSessionRequest request, IAgentOrchestrator orchestrator, CancellationToken ct) =>
        {
            var session = await orchestrator.CreateSessionAsync(request.UserId, request.AgentType, ct);
            return Results.Created($"/api/sessions/{session.SessionId}", new SessionResponse(session));
        })
        .WithName("CreateSession")
        .WithSummary("Create a new agent session");

        group.MapGet("/sessions/{id}", async (string id, IAgentOrchestrator orchestrator, CancellationToken ct) =>
        {
            var session = await orchestrator.GetSessionAsync(id, ct);
            return session is not null
                ? Results.Ok(new SessionResponse(session))
                : Results.NotFound();
        })
        .WithName("GetSession")
        .WithSummary("Get session information");

        group.MapDelete("/sessions/{id}", async (string id, IAgentOrchestrator orchestrator, CancellationToken ct) =>
        {
            await orchestrator.EndSessionAsync(id, ct);
            return Results.NoContent();
        })
        .WithName("EndSession")
        .WithSummary("End an agent session");

        app.MapHub<AgentHub>("/hubs/agent");

        return app;
    }
}

public sealed record CreateSessionRequest(string UserId, AgentType AgentType);

public sealed record SessionResponse
{
    public string SessionId { get; init; }
    public string UserId { get; init; }
    public string AgentType { get; init; }
    public string AgentName { get; init; }
    public string Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastActivityAt { get; init; }
    public int MessageCount { get; init; }

    public SessionResponse(AgentSession session)
    {
        SessionId = session.SessionId;
        UserId = session.UserId;
        AgentType = session.AgentType.ToString();
        AgentName = session.Agent.Name;
        Status = session.Status.ToString();
        CreatedAt = session.CreatedAt;
        LastActivityAt = session.LastActivityAt;
        MessageCount = session.ConversationHistory.Count;
    }
}
