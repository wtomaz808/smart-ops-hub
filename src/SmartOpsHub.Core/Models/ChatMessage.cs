namespace SmartOpsHub.Core.Models;

public enum ChatRole
{
    User,
    Assistant,
    System,
    Tool
}

public sealed record ChatMessage
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required ChatRole Role { get; init; }
    public required string Content { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string? ToolCallId { get; init; }
    public string? ToolName { get; init; }
    public List<FileAttachment> Attachments { get; init; } = [];
}

public sealed record FileAttachment
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public string? TextContent { get; init; }
    public string? Base64Data { get; init; }

    public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    public const int MaxFilesPerMessage = 5;

    public bool IsTextFile => ContentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
        || TextFileExtensions.Any(ext => FileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

    private static readonly string[] TextFileExtensions =
        [".cs", ".json", ".md", ".yaml", ".yml", ".xml", ".txt", ".bicep", ".csproj",
         ".sln", ".ps1", ".sh", ".py", ".js", ".ts", ".html", ".css", ".razor",
         ".config", ".props", ".targets", ".dockerfile", ".tf", ".env", ".gitignore", ".editorconfig"];
}
