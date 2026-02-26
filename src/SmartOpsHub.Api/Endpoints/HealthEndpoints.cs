using SmartOpsHub.Core.Interfaces;

namespace SmartOpsHub.Api.Endpoints;

public static class HealthEndpoints
{
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new
        {
            Status = "Healthy",
            Service = "SmartOpsHub.Api",
            Timestamp = DateTimeOffset.UtcNow
        }))
        .WithTags("Health")
        .WithName("HealthCheck")
        .WithSummary("Basic health check");

        app.MapGet("/health/ready", async (IMcpGateway mcpGateway, CancellationToken ct) =>
        {
            try
            {
                var healthStatus = await mcpGateway.GetHealthStatusAsync(ct);
                var allHealthy = healthStatus.Values.All(h => h);

                return Results.Ok(new
                {
                    Status = allHealthy ? "Ready" : "Degraded",
                    Service = "SmartOpsHub.Api",
                    Timestamp = DateTimeOffset.UtcNow,
                    Dependencies = healthStatus.ToDictionary(
                        kvp => kvp.Key.ToString(),
                        kvp => kvp.Value ? "Healthy" : "Unhealthy")
                });
            }
            catch (Exception ex)
            {
                return Results.Ok(new
                {
                    Status = "Unhealthy",
                    Service = "SmartOpsHub.Api",
                    Timestamp = DateTimeOffset.UtcNow,
                    Error = ex.Message
                });
            }
        })
        .WithTags("Health")
        .WithName("ReadinessCheck")
        .WithSummary("Readiness check including dependencies");

        return app;
    }
}
