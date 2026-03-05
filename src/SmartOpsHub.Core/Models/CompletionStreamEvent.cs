namespace SmartOpsHub.Core.Models;

/// <summary>
/// Base type for events emitted during a streaming completion.
/// </summary>
public abstract record CompletionStreamEvent;

/// <summary>
/// A text token yielded during streaming.
/// </summary>
public sealed record TextTokenEvent(string Text) : CompletionStreamEvent;

/// <summary>
/// Emitted at the end of a stream when Azure OpenAI requested tool calls instead of (or after) text.
/// </summary>
public sealed record ToolCallsCompleteEvent(IReadOnlyList<AiToolCallRequest> ToolCalls) : CompletionStreamEvent;

/// <summary>
/// A tool call request from the AI model.
/// </summary>
public sealed record AiToolCallRequest
{
    public required string Id { get; init; }
    public required string FunctionName { get; init; }
    public required string Arguments { get; init; }
}
