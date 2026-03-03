namespace SmartOpsHub.Core.Models;

public sealed record ModelOption(string DeploymentName, string DisplayName)
{
    public static readonly ModelOption Gpt4o = new("gpt-4o", "GPT-4o (2024-11-20)");
    public static readonly ModelOption Gpt41 = new("gpt-41", "GPT-4.1 (2025-04-14)");

    public static readonly IReadOnlyList<ModelOption> All = [Gpt41, Gpt4o];

    public static readonly ModelOption Default = Gpt41;

    public static ModelOption FromDeploymentName(string? deploymentName) =>
        All.FirstOrDefault(m => m.DeploymentName.Equals(deploymentName, StringComparison.OrdinalIgnoreCase))
        ?? Default;
}
