using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using SmartOpsHub.Api.Orchestration;
using SmartOpsHub.Core.Interfaces;
using SmartOpsHub.Core.Models;

namespace SmartOpsHub.Api.Tests;

#region Test Fakes

internal sealed class FakeAgentRegistry : IAgentRegistry
{
    private readonly List<AgentDefinition> _agents =
    [
        new() { Id = "github", Name = "GitHub", Type = AgentType.GitHub, Description = "GitHub agent", SystemPrompt = "You are GitHub agent." },
        new() { Id = "azure", Name = "Azure", Type = AgentType.Azure, Description = "Azure agent", SystemPrompt = "You are Azure agent." }
    ];

    public IReadOnlyList<AgentDefinition> GetAllAgents() => _agents;
    public AgentDefinition? GetAgent(AgentType type) => _agents.FirstOrDefault(a => a.Type == type);
    public IReadOnlyList<AgentDefinition> GetAgentsForUser(UserProfile user) => _agents;
}

internal sealed class FakeAiCompletionService : IAiCompletionService
{
    public string NextResponse { get; set; } = "Test AI response";
    public bool ShouldThrow { get; set; }

    public Task<string> GetCompletionAsync(IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<McpToolDefinition>? availableTools = null, CancellationToken cancellationToken = default)
    {
        if (ShouldThrow) throw new InvalidOperationException("AI service unavailable");
        return Task.FromResult(NextResponse);
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<McpToolDefinition>? availableTools = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (ShouldThrow) throw new InvalidOperationException("AI service unavailable");
        foreach (var word in NextResponse.Split(' '))
        {
            yield return word + " ";
            await Task.CompletedTask;
        }
    }
}

internal sealed class FakeMcpGateway : IMcpGateway
{
    public Task<IMcpClient> GetClientAsync(AgentType agentType, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IMcpClient>(new FakeMcpClient());
    }

    public Task<IReadOnlyDictionary<AgentType, bool>> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyDictionary<AgentType, bool>>(
            new Dictionary<AgentType, bool> { [AgentType.GitHub] = true });
    }
}

internal sealed class FakeMcpClient : IMcpClient
{
    public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<McpToolDefinition>>([new() { Name = "test_tool", Description = "A test tool" }]);

    public Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken cancellationToken = default)
        => Task.FromResult(new McpToolResult { ToolCallId = toolCall.Id, Content = "test result" });

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
}

internal sealed class FakeSessionRepository : ISessionRepository
{
    private readonly Dictionary<string, AgentSession> _sessions = [];

    public Task<AgentSession?> GetByIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task<IReadOnlyList<AgentSession>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AgentSession>>(_sessions.Values.Where(s => s.UserId == userId).ToList());

    public Task SaveAsync(AgentSession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.SessionId] = session;
        return Task.CompletedTask;
    }

    public Task UpdateStatusAsync(string sessionId, AgentSessionStatus status, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
            session.Status = status;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.Remove(sessionId);
        return Task.CompletedTask;
    }
}

internal sealed class FakeConversationRepository : IConversationRepository
{
    private readonly Dictionary<string, List<ChatMessage>> _messages = [];

    public Task AddMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        if (!_messages.TryGetValue(sessionId, out var list))
        {
            list = [];
            _messages[sessionId] = list;
        }
        list.Add(message);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ChatMessage>>(_messages.GetValueOrDefault(sessionId, []).ToList());

    public Task DeleteBySessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _messages.Remove(sessionId);
        return Task.CompletedTask;
    }
}

#endregion

public class AgentOrchestratorTests
{
    private static AgentOrchestrator CreateOrchestrator(
        FakeAiCompletionService? ai = null,
        FakeAgentRegistry? registry = null,
        FakeMcpGateway? gateway = null,
        FakeSessionRepository? sessionRepo = null,
        FakeConversationRepository? convRepo = null)
    {
        return new AgentOrchestrator(
            registry ?? new FakeAgentRegistry(),
            ai ?? new FakeAiCompletionService(),
            gateway ?? new FakeMcpGateway(),
            sessionRepo ?? new FakeSessionRepository(),
            convRepo ?? new FakeConversationRepository(),
            NullLogger<AgentOrchestrator>.Instance);
    }

    [Fact]
    public async Task CreateSessionAsync_Returns_NewSession()
    {
        var orchestrator = CreateOrchestrator();
        var session = await orchestrator.CreateSessionAsync("user-1", AgentType.GitHub);

        Assert.NotNull(session);
        Assert.Equal("user-1", session.UserId);
        Assert.Equal(AgentType.GitHub, session.AgentType);
        Assert.NotEmpty(session.SessionId);
    }

    [Fact]
    public async Task CreateSessionAsync_AddsSystemPrompt()
    {
        var orchestrator = CreateOrchestrator();
        var session = await orchestrator.CreateSessionAsync("user-1", AgentType.GitHub);

        Assert.Contains(session.ConversationHistory, m => m.Role == ChatRole.System);
    }

    [Fact]
    public async Task CreateSessionAsync_InvalidAgentType_Throws()
    {
        var registry = new FakeAgentRegistry();
        var orchestrator = CreateOrchestrator(registry: registry);

        // Personal agent is not in our fake registry
        await Assert.ThrowsAsync<ArgumentException>(
            () => orchestrator.CreateSessionAsync("user-1", AgentType.Personal));
    }

    [Fact]
    public async Task GetSessionAsync_ExistingSession_Returns_Session()
    {
        var orchestrator = CreateOrchestrator();
        var created = await orchestrator.CreateSessionAsync("user-1", AgentType.GitHub);

        var found = await orchestrator.GetSessionAsync(created.SessionId);

        Assert.NotNull(found);
        Assert.Equal(created.SessionId, found!.SessionId);
    }

    [Fact]
    public async Task GetSessionAsync_NonExistentSession_Returns_Null()
    {
        var orchestrator = CreateOrchestrator();
        var result = await orchestrator.GetSessionAsync("nonexistent-id");
        Assert.Null(result);
    }

    [Fact]
    public async Task EndSessionAsync_RemovesSession()
    {
        var orchestrator = CreateOrchestrator();
        var session = await orchestrator.CreateSessionAsync("user-1", AgentType.GitHub);

        await orchestrator.EndSessionAsync(session.SessionId);

        var result = await orchestrator.GetSessionAsync(session.SessionId);
        Assert.Null(result);
    }

    [Fact]
    public async Task EndSessionAsync_NonExistentSession_DoesNotThrow()
    {
        var orchestrator = CreateOrchestrator();
        await orchestrator.EndSessionAsync("nonexistent-id"); // should not throw
    }

    [Fact]
    public async Task ProcessMessageAsync_Returns_AssistantMessage()
    {
        var ai = new FakeAiCompletionService { NextResponse = "Hello from AI" };
        var orchestrator = CreateOrchestrator(ai: ai);
        var session = await orchestrator.CreateSessionAsync("user-1", AgentType.GitHub);

        var response = await orchestrator.ProcessMessageAsync(session.SessionId, "Hello");

        Assert.Equal(ChatRole.Assistant, response.Role);
        Assert.Equal("Hello from AI", response.Content);
    }

    [Fact]
    public async Task ProcessMessageAsync_AddsUserAndAssistantMessages()
    {
        var orchestrator = CreateOrchestrator();
        var session = await orchestrator.CreateSessionAsync("user-1", AgentType.GitHub);

        await orchestrator.ProcessMessageAsync(session.SessionId, "Test message");

        // System + User + Assistant = 3
        Assert.Equal(3, session.ConversationHistory.Count);
        Assert.Equal(ChatRole.System, session.ConversationHistory[0].Role);
        Assert.Equal(ChatRole.User, session.ConversationHistory[1].Role);
        Assert.Equal(ChatRole.Assistant, session.ConversationHistory[2].Role);
    }

    [Fact]
    public async Task ProcessMessageAsync_SetsStatusToIdleAfterSuccess()
    {
        var orchestrator = CreateOrchestrator();
        var session = await orchestrator.CreateSessionAsync("user-1", AgentType.GitHub);

        await orchestrator.ProcessMessageAsync(session.SessionId, "Hello");

        Assert.Equal(AgentSessionStatus.Idle, session.Status);
    }

    [Fact]
    public async Task ProcessMessageAsync_SetsStatusToErrorOnFailure()
    {
        var ai = new FakeAiCompletionService { ShouldThrow = true };
        var orchestrator = CreateOrchestrator(ai: ai);
        var session = await orchestrator.CreateSessionAsync("user-1", AgentType.GitHub);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.ProcessMessageAsync(session.SessionId, "Hello"));

        Assert.Equal(AgentSessionStatus.Error, session.Status);
    }

    [Fact]
    public async Task ProcessMessageAsync_NonExistentSession_Throws()
    {
        var orchestrator = CreateOrchestrator();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => orchestrator.ProcessMessageAsync("nonexistent-id", "Hello"));
    }

    [Fact]
    public async Task ProcessMessageAsync_MultipleMessages_BuildsHistory()
    {
        var ai = new FakeAiCompletionService();
        var orchestrator = CreateOrchestrator(ai: ai);
        var session = await orchestrator.CreateSessionAsync("user-1", AgentType.GitHub);

        ai.NextResponse = "Response 1";
        await orchestrator.ProcessMessageAsync(session.SessionId, "Message 1");

        ai.NextResponse = "Response 2";
        await orchestrator.ProcessMessageAsync(session.SessionId, "Message 2");

        // System + (User+Assistant) * 2 = 5
        Assert.Equal(5, session.ConversationHistory.Count);
    }

    // ─── Streaming Tests ──────────────────────────────────────────

    [Fact]
    public async Task StreamMessageAsync_YieldsTokens()
    {
        var ai = new FakeAiCompletionService { NextResponse = "Hello from AI" };
        var orchestrator = CreateOrchestrator(ai: ai);
        var session = await orchestrator.CreateSessionAsync("user-1", AgentType.GitHub);

        var tokens = new List<string>();
        await foreach (var token in orchestrator.StreamMessageAsync(session.SessionId, "Hi"))
        {
            tokens.Add(token);
        }

        Assert.NotEmpty(tokens);
        Assert.Equal("Hello from AI ", string.Concat(tokens));
    }

    [Fact]
    public async Task StreamMessageAsync_PersistsFullResponse()
    {
        var ai = new FakeAiCompletionService { NextResponse = "Streamed response" };
        var convRepo = new FakeConversationRepository();
        var orchestrator = CreateOrchestrator(ai: ai, convRepo: convRepo);
        var session = await orchestrator.CreateSessionAsync("user-1", AgentType.GitHub);

        await foreach (var _ in orchestrator.StreamMessageAsync(session.SessionId, "Hi")) { }

        // System + User + Assistant = 3
        Assert.Equal(3, session.ConversationHistory.Count);
        Assert.Equal(ChatRole.Assistant, session.ConversationHistory[2].Role);
        Assert.Equal("Streamed response ", session.ConversationHistory[2].Content);
    }

    [Fact]
    public async Task StreamMessageAsync_SetsStatusToIdleAfterComplete()
    {
        var orchestrator = CreateOrchestrator();
        var session = await orchestrator.CreateSessionAsync("user-1", AgentType.GitHub);

        await foreach (var _ in orchestrator.StreamMessageAsync(session.SessionId, "Hi")) { }

        Assert.Equal(AgentSessionStatus.Idle, session.Status);
    }

    [Fact]
    public async Task StreamMessageAsync_NonExistentSession_Throws()
    {
        var orchestrator = CreateOrchestrator();

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await foreach (var _ in orchestrator.StreamMessageAsync("nonexistent-id", "Hi")) { }
        });
    }

    [Fact]
    public async Task StreamMessageAsync_MultipleTokens_ConcatenateToFullResponse()
    {
        var ai = new FakeAiCompletionService { NextResponse = "one two three" };
        var orchestrator = CreateOrchestrator(ai: ai);
        var session = await orchestrator.CreateSessionAsync("user-1", AgentType.GitHub);

        var tokens = new List<string>();
        await foreach (var token in orchestrator.StreamMessageAsync(session.SessionId, "Count"))
        {
            tokens.Add(token);
        }

        Assert.Equal(3, tokens.Count);
        Assert.Equal("one ", tokens[0]);
        Assert.Equal("two ", tokens[1]);
        Assert.Equal("three ", tokens[2]);
    }
}
