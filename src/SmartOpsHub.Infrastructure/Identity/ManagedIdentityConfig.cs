namespace SmartOpsHub.Infrastructure.Identity;

public sealed record ManagedIdentityConfig
{
    public string? ClientId { get; init; }
    public string? TenantId { get; init; }
}
